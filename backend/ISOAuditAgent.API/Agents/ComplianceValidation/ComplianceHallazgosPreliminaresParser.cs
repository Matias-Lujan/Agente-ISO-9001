// ============================================================================
//  ComplianceHallazgosPreliminaresParser — ComplianceValidation (Parte 4.D)
// ----------------------------------------------------------------------------
//  Parsea la respuesta JSON del LLM a una LISTA de HallazgoPreliminar. El
//  esqueleto de parseo + validaciones (estructura, ids expuestos, campos
//  requeridos, falla ruidosa) vive en HallazgoPreliminarLlmParser, compartido
//  con ConsistencyVerification. Acá solo se define la política propia de este
//  nodo: devuelve la lista plana (el nodo la fusiona con los determinísticos,
//  no la envuelve) y fuerza OrigenRegla = Tailoring.
//
//  OrigenRegla ACOTADO A "Tailoring": el alcance del LLM en este nodo es solo
//  detectar incoherencias entre la URL declarada en el tailoring y el archivo
//  encontrado. La regla violada es SIEMPRE del tailoring. El prompt lo enuncia
//  explícito; este parser lo fuerza por defensa. Si el LLM emite Procedimiento
//  o Template, es bug de clasificación y se rechaza ruidosamente. Si en el
//  futuro el alcance del LLM se amplía, ampliar el resolver de abajo.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.Shared;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class ComplianceHallazgosPreliminaresParser
{
    /// <summary>
    /// Parsea la respuesta del LLM y devuelve los hallazgos como lista plana.
    /// El nodo los fusiona con los determinísticos antes de armar el
    /// HallazgosPreliminares final.
    /// </summary>
    /// <param name="textoLlm">JSON crudo devuelto por el LLM.</param>
    /// <param name="idsExpuestos">
    /// IDs de los artefactos que efectivamente se incluyeron en el prompt del
    /// LLM (el nodo filtra a Exigible + Aplica + Encontrado). Cualquier hallazgo
    /// sobre un id fuera de este conjunto es alucinación y se rechaza.
    /// </param>
    public static List<HallazgoPreliminar> Parsear(
        string textoLlm,
        IReadOnlySet<int> idsExpuestos)
        => HallazgoPreliminarLlmParser.Parsear(textoLlm, idsExpuestos, ResolverOrigenRegla);

    // OrigenRegla acotado a Tailoring (ver cabecera).
    private static OrigenRegla ResolverOrigenRegla(string origenRegla, int indice, int artefactoId)
    {
        if (!string.Equals(origenRegla.Trim(), "Tailoring", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                $"'origenRegla' inválida para ComplianceValidation: '{origenRegla}'. " +
                "El único valor permitido para hallazgos del LLM en este nodo es Tailoring.");
        }

        return OrigenRegla.Tailoring;
    }
}
