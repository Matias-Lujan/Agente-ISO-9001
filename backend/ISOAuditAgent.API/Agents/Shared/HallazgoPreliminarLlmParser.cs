// ============================================================================
//  HallazgoPreliminarLlmParser — esqueleto común de parseo de hallazgos del LLM
// ----------------------------------------------------------------------------
//  Compartido por ComplianceValidation y ConsistencyVerification: ambos reciben
//  del LLM un objeto {"hallazgos":[...]} y lo convierten en HallazgoPreliminar
//  con las MISMAS validaciones y la MISMA política de "fallar ruidoso".
//
//  Lo único que difiere entre los dos nodos es cómo se resuelve OrigenRegla:
//   - ComplianceValidation: fuerza Tailoring (rechaza el resto).
//   - ConsistencyVerification: acepta Procedimiento/Template/Tailoring.
//  Por eso la resolución de OrigenRegla entra por delegate; el resto del parseo
//  (estructura, ids expuestos, campos requeridos) vive acá una sola vez.
//
//  FALLA RUIDOSO ante cualquier desalineación (JSON inválido, estructura
//  inesperada, id no expuesto al LLM, campos vacíos). No se silencian errores
//  del LLM: ocultarlos taparía bugs reales del modelo.
//
//  El caller decide qué hacer con la lista (envolver, deduplicar, etc.).
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.Shared;

internal static class HallazgoPreliminarLlmParser
{
    /// <summary>
    /// Parsea {"hallazgos":[...]} a una lista plana de HallazgoPreliminar.
    /// </summary>
    /// <param name="textoLlm">JSON crudo devuelto por el LLM.</param>
    /// <param name="idsExpuestos">
    /// IDs que efectivamente se le presentaron al LLM en el prompt. Un hallazgo
    /// sobre un id fuera de este conjunto es alucinación y se rechaza.
    /// </param>
    /// <param name="resolverOrigenRegla">
    /// Resuelve el string 'origenRegla' del LLM a <see cref="OrigenRegla"/>.
    /// Recibe (origenRegla, indice, artefactoId) para poder lanzar un error
    /// con contexto si el valor no es válido para el nodo.
    /// </param>
    public static List<HallazgoPreliminar> Parsear(
        string textoLlm,
        IReadOnlySet<int> idsExpuestos,
        Func<string, int, int, OrigenRegla> resolverOrigenRegla)
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

            var resultado = new List<HallazgoPreliminar>();

            var indice = 0;
            foreach (var item in hallazgosNode.EnumerateArray())
            {
                resultado.Add(ParsearUno(item, indice, idsExpuestos, resolverOrigenRegla));
                indice++;
            }

            return resultado;
        }
    }

    private static HallazgoPreliminar ParsearUno(
        JsonElement item,
        int indice,
        IReadOnlySet<int> idsExpuestos,
        Func<string, int, int, OrigenRegla> resolverOrigenRegla)
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
                "El LLM no puede emitir hallazgos sobre artefactos que no recibió.");
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

        var origenRegla = resolverOrigenRegla(origenReglaStr, indice, artefactoId);

        return new HallazgoPreliminar(artefactoId, descripcion, justificacion, origenRegla);
    }
}
