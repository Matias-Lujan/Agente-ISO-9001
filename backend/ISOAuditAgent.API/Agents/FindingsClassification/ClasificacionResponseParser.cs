// ============================================================================
//  ClasificacionResponseParser (Parte 4.A — corregido)
// ----------------------------------------------------------------------------
//  Convierte la respuesta JSON del LLM en un HallazgosClasificados tipado.
//
//  ORIGEN: AgenteAuditoria/Services/ClasificacionResponseParser.cs del diff
//  de classification, adaptado a contratos del monorepo y corregido.
//
//  IDENTIDAD POR INDICE ESTABLE, NO POR ArtefactoEsperadoId.
//  La unidad de 1-a-1 que pide el contrato 5 es el HallazgoPreliminar, no el
//  artefacto. Un mismo ArtefactoEsperadoId puede tener múltiples preliminares
//  (distintos agentes, distintas reglas, distintas OrigenRegla). El nodo
//  arma una lista plana con indice 0..N-1; el LLM debe devolver el mismo
//  indice por cada item; el parser arma la salida indexando por posición.
//
//  REGLA DE NEGOCIO QUE EL PARSER FUERZA (red de seguridad):
//  Si el HallazgoPreliminar original tiene OrigenRegla != Procedimiento y el
//  LLM lo clasificó como NC, se degrada a OM. El prompt también enuncia esta
//  regla; el código la fuerza por si el LLM no la respeta.
//
//  FALLAR RUIDOSO ANTE DESALINEACIÓN.
//  Cualquiera de estas situaciones lanza InvalidOperationException:
//   - JSON inválido o no es array.
//   - Cantidad de items distinta a la cantidad de preliminares.
//   - indice fuera de rango [0, N-1].
//   - indice duplicado.
//   - indice faltante en la respuesta.
//  La invariante 1-a-1 vive en este parser. No se ignoran fallas del LLM:
//  el comportamiento permisivo del diff original (devolver lista vacía u
//  ocultar items faltantes) tapaba bugs reales del modelo.
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.FindingsClassification;

internal static class ClasificacionResponseParser
{
    /// <summary>
    /// Parsea la respuesta del LLM y arma <see cref="HallazgosClasificados"/>.
    /// </summary>
    /// <param name="textoLlm">JSON crudo devuelto por el LLM.</param>
    /// <param name="preliminaresPlanos">
    /// Lista plana de (HallazgoPreliminar, AgenteOrigen) con el MISMO orden e
    /// índices que se le presentaron al LLM en el prompt. La posición es la
    /// identidad: indice = 0 corresponde al primer item de esta lista.
    /// </param>
    /// <param name="auditoriaId">Id de la auditoría. Va en el DTO de salida.</param>
    public static HallazgosClasificados Parsear(
        string textoLlm,
        IReadOnlyList<(HallazgoPreliminar Hallazgo, AgenteOrigen Origen)> preliminaresPlanos,
        int auditoriaId)
    {
        var jsonLimpio = textoLlm
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(jsonLimpio);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Respuesta del LLM no es JSON válido: {ex.Message}", ex);
        }

        using (doc)
        {
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    $"Respuesta del LLM inválida: se esperaba un array JSON en la raíz, " +
                    $"se recibió {doc.RootElement.ValueKind}.");
            }

            var items = doc.RootElement.EnumerateArray().ToList();
            var esperados = preliminaresPlanos.Count;

            if (items.Count != esperados)
            {
                throw new InvalidOperationException(
                    $"Cantidad incorrecta en la respuesta del LLM: " +
                    $"esperaba {esperados} items, recibió {items.Count}.");
            }

            // Indexar por indice y validar 1-a-1 exacto contra la lista plana.
            var resultadoPorIndice = new HallazgoClasificado?[esperados];

            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];

                if (!item.TryGetProperty("indice", out var indiceProp) ||
                    indiceProp.ValueKind != JsonValueKind.Number)
                {
                    throw new InvalidOperationException(
                        $"Item {i} de la respuesta del LLM no tiene la propiedad 'indice' " +
                        $"(o no es número).");
                }

                var indice = indiceProp.GetInt32();

                if (indice < 0 || indice >= esperados)
                {
                    throw new InvalidOperationException(
                        $"Indice {indice} fuera de rango [0, {esperados - 1}] en la " +
                        $"respuesta del LLM.");
                }

                if (resultadoPorIndice[indice] is not null)
                {
                    throw new InvalidOperationException(
                        $"Indice {indice} aparece duplicado en la respuesta del LLM.");
                }

                var tipoStr = item.TryGetProperty("tipo", out var t)
                    ? t.GetString() ?? string.Empty
                    : string.Empty;

                var justificacionLlm = item.TryGetProperty("justificacion", out var j)
                    ? j.GetString() ?? string.Empty
                    : string.Empty;

                var (hallazgoOriginal, agenteOrigen) = preliminaresPlanos[indice];

                resultadoPorIndice[indice] = new HallazgoClasificado(
                    ArtefactoEsperadoId: hallazgoOriginal.ArtefactoEsperadoId,
                    Tipo: ResolverTipo(tipoStr, hallazgoOriginal.OrigenRegla),
                    Descripcion: hallazgoOriginal.Descripcion,
                    Justificacion: hallazgoOriginal.OrigenRegla == OrigenRegla.Procedimiento
                        ? hallazgoOriginal.Justificacion
                        : string.IsNullOrWhiteSpace(justificacionLlm)
                            ? hallazgoOriginal.Justificacion
                            : justificacionLlm,
                    AgenteOrigen: agenteOrigen);
            }

            // Si después de iterar todos los items quedó algún null, es porque
            // un indice estaba ausente. Es redundante con la validación de
            // cantidad + sin duplicados, pero lo dejamos explícito por seguridad
            // y para producir un mensaje de error claro.
            for (var i = 0; i < esperados; i++)
            {
                if (resultadoPorIndice[i] is null)
                {
                    throw new InvalidOperationException(
                        $"Indice {i} ausente en la respuesta del LLM.");
                }
            }

            // El cast es seguro: el chequeo anterior garantiza que no hay null.
            var resultado = resultadoPorIndice.Cast<HallazgoClasificado>().ToList();

            return new HallazgosClasificados(auditoriaId, resultado);
        }
    }

    /// <summary>
    /// Convierte el string de tipo al enum <see cref="TipoHallazgo"/> y aplica
    /// la regla de oro: si OrigenRegla != Procedimiento, no puede ser NC.
    /// </summary>
    private static TipoHallazgo ResolverTipo(string tipo, OrigenRegla origenRegla)
    {
        var clasificadoPorLlm = tipo.Trim().ToUpperInvariant() switch
        {
            "NC" => TipoHallazgo.NC,
            "OBS" => TipoHallazgo.OBS,
            "OM" => TipoHallazgo.OM,
            // Si el LLM devuelve algo no reconocido, default conservador a OBS.
            _ => TipoHallazgo.OBS
        };

        // Regla de oro determinística: degradar NC a OM solo para OrigenRegla.Tailoring.
        // Procedimiento y Template pueden producir NC (ERS 4.2).
        // Tailoring permanece techado a OM: las reglas de negocio del tailoring
        // son acuerdos de proyecto, no requisitos del procedimiento.
        if (origenRegla == OrigenRegla.Tailoring && clasificadoPorLlm == TipoHallazgo.NC)
        {
            return TipoHallazgo.OM;
        }

        return clasificadoPorLlm;
    }
}
