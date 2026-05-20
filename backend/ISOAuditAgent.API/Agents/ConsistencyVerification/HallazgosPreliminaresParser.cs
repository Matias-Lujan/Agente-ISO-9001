// ============================================================================
//  HallazgosPreliminaresParser — ConsistencyVerification (Parte 4.B)
// ----------------------------------------------------------------------------
//  Convierte la respuesta JSON del LLM en un HallazgosPreliminares tipado.
//
//  ORIGEN: Agents/ConsistencyVerification/ConsistencyVerificationAgentService.
//  MapHallazgos del diff, adaptado a:
//   - System.Text.Json directo (sin tipos intermedios GeminiHallazgoResponse).
//   - Sin ILogger (DI es Chat D).
//   - FALLAR RUIDOSO ante hallazgos malformados o inexistentes (el diff los
//     descartaba con LogWarning silencioso; eso oculta bugs del LLM).
//
//  DIFERENCIA CON 4.A (FindingsClassification):
//  En 4.A había invariante 1-a-1 estricta (contrato 5 línea 307), así que el
//  prompt usaba un "indice estable". Acá NO hay 1-a-1: el LLM descubre
//  hallazgos donde los ve, un artefacto puede tener 0, 1 o N hallazgos. La
//  identidad del hallazgo es ArtefactoEsperadoId + Descripcion, no un índice.
//  Validamos cada hallazgo por separado contra los IDs que el LLM efectivamente
//  vio en el prompt (idsExpuestos), no contra todos los IDs del input.
//
//  CASOS QUE LANZAN InvalidOperationException:
//   - JSON inválido.
//   - Estructura raíz no es objeto con propiedad "hallazgos".
//   - "hallazgos" no es array.
//   - Hallazgo con ArtefactoEsperadoId NO presente en idsExpuestos. Esto es
//     clave: idsExpuestos son los IDs que efectivamente vio el LLM en el
//     prompt, NO todos los IDs del input. Aceptar un hallazgo sobre un
//     artefacto que el LLM no recibió es aceptar una alucinación pura. Y si
//     el LLM emitiera un hallazgo sobre un artefacto Faltante / Pendiente,
//     estaría pisando responsabilidad de ComplianceValidation.
//   - Hallazgo con Descripcion o Justificacion vacías.
//   - OrigenRegla no reconocida (valores válidos: Procedimiento, Template,
//     Tailoring — case-insensitive).
//
//  LISTA VACÍA ES VÁLIDA: si {"hallazgos": []} se devuelve HallazgosPreliminares
//  con lista vacía. El contrato 3 lo admite explícitamente.
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class HallazgosPreliminaresParser
{
    /// <summary>
    /// Parsea la respuesta del LLM y arma <see cref="HallazgosPreliminares"/>
    /// con <see cref="AgenteOrigen.ConsistencyVerification"/>.
    /// </summary>
    /// <param name="textoLlm">JSON crudo devuelto por el LLM.</param>
    /// <param name="idsExpuestos">
    /// IDs de los artefactos que efectivamente se incluyeron en el prompt del
    /// LLM. NO es la lista completa de artefactos del input — el nodo filtra
    /// a Exigible + Encontrado antes de armar el prompt y debe pasar acá la
    /// misma lista que vio el LLM. Cualquier hallazgo emitido sobre un id
    /// fuera de este conjunto es alucinación y se rechaza con excepción.
    /// </param>
    /// <param name="auditoriaId">Id de la auditoría. Va en el DTO de salida.</param>
    public static HallazgosPreliminares Parsear(
        string textoLlm,
        IReadOnlySet<int> idsExpuestos,
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
            if (doc.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new InvalidOperationException(
                    "Respuesta del LLM inválida: se esperaba un objeto JSON con " +
                    $"propiedad 'hallazgos', se recibió {doc.RootElement.ValueKind}.");
            }

            if (!doc.RootElement.TryGetProperty("hallazgos", out var hallazgosNode) ||
                hallazgosNode.ValueKind != JsonValueKind.Array)
            {
                throw new InvalidOperationException(
                    "Respuesta del LLM inválida: falta la propiedad 'hallazgos' " +
                    "o no es array.");
            }

            // idsExpuestos son los IDs que el LLM efectivamente vio en el
            // prompt. El nodo es responsable de pasarlos coherentes con lo
            // que armó el ConstruirPrompt.
            var resultado = new List<HallazgoPreliminar>();

            var indice = 0;
            foreach (var item in hallazgosNode.EnumerateArray())
            {
                var hallazgo = ParsearUno(item, indice, idsExpuestos);
                resultado.Add(hallazgo);
                indice++;
            }

            return new HallazgosPreliminares(
                auditoriaId,
                AgenteOrigen.ConsistencyVerification,
                resultado);
        }
    }

    private static HallazgoPreliminar ParsearUno(
        JsonElement item, int indice, IReadOnlySet<int> idsExpuestos)
    {
        if (item.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice} de la respuesta del LLM no es objeto.");
        }

        if (!item.TryGetProperty("artefactoEsperadoId", out var idProp) ||
            idProp.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice}: falta 'artefactoEsperadoId' o no es número.");
        }

        var artefactoId = idProp.GetInt32();

        if (!idsExpuestos.Contains(artefactoId))
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice}: ArtefactoEsperadoId={artefactoId} no estaba " +
                $"entre los artefactos que se le presentaron al LLM en el prompt. " +
                $"El LLM no puede emitir hallazgos sobre artefactos que no recibió.");
        }

        var descripcion = item.TryGetProperty("descripcion", out var d)
            ? d.GetString()?.Trim() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(descripcion))
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                "'descripcion' vacía o ausente.");
        }

        var justificacion = item.TryGetProperty("justificacion", out var j)
            ? j.GetString()?.Trim() ?? string.Empty
            : string.Empty;

        if (string.IsNullOrWhiteSpace(justificacion))
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                "'justificacion' vacía o ausente.");
        }

        var origenReglaStr = item.TryGetProperty("origenRegla", out var or)
            ? or.GetString() ?? string.Empty
            : string.Empty;

        var origenRegla = origenReglaStr.Trim().ToLowerInvariant() switch
        {
            "procedimiento" => OrigenRegla.Procedimiento,
            "template" => OrigenRegla.Template,
            "tailoring" => OrigenRegla.Tailoring,
            _ => throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                $"'origenRegla' inválida: '{origenReglaStr}'. " +
                "Valores válidos: Procedimiento, Template, Tailoring.")
        };

        return new HallazgoPreliminar(
            artefactoId,
            descripcion,
            justificacion,
            origenRegla);
    }
}