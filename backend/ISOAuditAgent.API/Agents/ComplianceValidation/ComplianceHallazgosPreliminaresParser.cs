// ============================================================================
//  ComplianceHallazgosPreliminaresParser — ComplianceValidation (Parte 4.D)
// ----------------------------------------------------------------------------
//  Convierte la respuesta JSON del LLM en una LISTA de HallazgoPreliminar.
//  No envuelve en HallazgosPreliminares (eso lo hace el nodo, fusionando con
//  los determinísticos).
//
//  ESTRUCTURA SIMILAR A 4.B (ConsistencyHallazgosPreliminaresParser):
//   - System.Text.Json directo, sin tipos intermedios.
//   - FALLA RUIDOSO ante hallazgos malformados o IDs no expuestos al LLM.
//   - Lista vacía válida.
//   - idsExpuestos: IDs que se le presentaron al LLM en el prompt (el nodo
//     filtra a Exigible + Aplica + Encontrado).
//
//  DIFERENCIAS RESPECTO DEL DE 4.B:
//   - Devuelve List<HallazgoPreliminar>, no HallazgosPreliminares. El nodo
//     fusiona con los determinísticos antes de envolver.
//   - OrigenRegla ACOTADO A "Tailoring".
//     El alcance del LLM en este nodo es solo "detectar incoherencias entre
//     la URL declarada en el tailoring y el archivo encontrado". La regla
//     violada en esa detección es SIEMPRE una regla del tailoring (no del
//     procedimiento, no del template). El prompt es explícito al respecto;
//     este parser lo fuerza por defensa. Si el LLM emite Procedimiento o
//     Template, es bug de clasificación del LLM y se rechaza ruidosamente.
//     Si en el futuro el alcance del LLM se amplía, este parser debe ampliar
//     el switch.
// ============================================================================

using System.Text.Json;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class ComplianceHallazgosPreliminaresParser
{
    /// <summary>
    /// Parsea la respuesta del LLM y devuelve los hallazgos del LLM como lista
    /// plana. El nodo los fusiona con los determinísticos antes de armar el
    /// HallazgosPreliminares final.
    /// </summary>
    /// <param name="textoLlm">JSON crudo devuelto por el LLM.</param>
    /// <param name="idsExpuestos">
    /// IDs de los artefactos que efectivamente se incluyeron en el prompt del
    /// LLM. NO es la lista completa del input — el nodo filtra a Exigible +
    /// Aplica + Encontrado antes del prompt. Cualquier hallazgo emitido sobre
    /// un id fuera de este conjunto es alucinación y se rechaza con excepción.
    /// </param>
    public static List<HallazgoPreliminar> Parsear(
        string textoLlm,
        IReadOnlySet<int> idsExpuestos)
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
                resultado.Add(ParsearUno(item, indice, idsExpuestos));
                indice++;
            }

            return resultado;
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

        // OrigenRegla acotado a Tailoring: el alcance del LLM en este nodo es
        // solo detectar incoherencias entre la URL declarada en el tailoring
        // y el archivo encontrado. La regla violada es SIEMPRE una regla del
        // tailoring. El prompt lo enuncia explícito; este chequeo lo fuerza.
        if (!string.Equals(origenReglaStr.Trim(), "Tailoring", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                $"'origenRegla' inválida para ComplianceValidation: '{origenReglaStr}'. " +
                "El único valor permitido para hallazgos del LLM en este nodo es Tailoring.");
        }

        return new HallazgoPreliminar(
            artefactoId,
            descripcion,
            justificacion,
            OrigenRegla.Tailoring);
    }
}
