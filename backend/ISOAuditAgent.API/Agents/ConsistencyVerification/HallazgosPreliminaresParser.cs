// ============================================================================
//  HallazgosPreliminaresParser — ConsistencyVerification (Parte 4.B)
// ----------------------------------------------------------------------------
//  Parsea la respuesta JSON del LLM a un HallazgosPreliminares tipado. El
//  esqueleto de parseo + validaciones (estructura, ids expuestos, campos
//  requeridos, falla ruidosa) vive en HallazgoPreliminarLlmParser, compartido
//  con ComplianceValidation. Acá solo se define la política propia de este
//  nodo: OrigenRegla puede ser Procedimiento/Template/Tailoring, se deduplica
//  y se envuelve en HallazgosPreliminares (AgenteOrigen.ConsistencyVerification).
//
//  SIN invariante 1-a-1 (a diferencia de FindingsClassification): un artefacto
//  puede tener 0, 1 o N hallazgos. La identidad es ArtefactoEsperadoId +
//  Descripcion + Justificacion, que es además la clave del dedup.
//
//  LISTA VACÍA ES VÁLIDA: {"hallazgos": []} → HallazgosPreliminares con lista
//  vacía (el contrato 3 lo admite explícitamente).
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.Shared;
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
    /// IDs de los artefactos que efectivamente vio el LLM en el prompt (el nodo
    /// filtra a Exigible + Encontrado). Cualquier hallazgo sobre un id fuera de
    /// este conjunto es alucinación y se rechaza.
    /// </param>
    /// <param name="auditoriaId">Id de la auditoría. Va en el DTO de salida.</param>
    public static HallazgosPreliminares Parsear(
        string textoLlm,
        IReadOnlySet<int> idsExpuestos,
        int auditoriaId)
    {
        var hallazgos = HallazgoPreliminarLlmParser.Parsear(
            textoLlm, idsExpuestos, ResolverOrigenRegla);

        // Deduplicar: el LLM puede emitir el mismo hallazgo varias veces si la
        // sección aparece repetida en el documento parseado.
        var sinDuplicados = hallazgos
            .DistinctBy(h => (h.ArtefactoEsperadoId, h.Descripcion, h.Justificacion))
            .ToList();

        return new HallazgosPreliminares(
            auditoriaId,
            AgenteOrigen.ConsistencyVerification,
            sinDuplicados);
    }

    // OrigenRegla: Procedimiento / Template / Tailoring (case-insensitive).
    private static OrigenRegla ResolverOrigenRegla(string origenRegla, int indice, int artefactoId) =>
        origenRegla.Trim().ToLowerInvariant() switch
        {
            "procedimiento" => OrigenRegla.Procedimiento,
            "template" => OrigenRegla.Template,
            "tailoring" => OrigenRegla.Tailoring,
            _ => throw new InvalidOperationException(
                $"Hallazgo #{indice} (ArtefactoEsperadoId={artefactoId}): " +
                $"'origenRegla' inválida: '{origenRegla}'. " +
                "Valores válidos: Procedimiento, Template, Tailoring.")
        };
}
