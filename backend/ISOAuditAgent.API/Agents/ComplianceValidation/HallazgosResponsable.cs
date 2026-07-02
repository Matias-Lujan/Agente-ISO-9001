// ============================================================================
//  HallazgosResponsable — ComplianceValidation
// ----------------------------------------------------------------------------
//  Valida la completitud del tailoring respecto del campo "Responsable" de la
//  PORTADA del FR 29 (un campo único del proyecto, NO la columna "Responsable"
//  por artefacto de la tabla).
//
//  REGLA (acordada con el cliente):
//   - Si el Responsable de la portada está cargado → sin hallazgo.
//   - Si está vacío → OBS, colgada del artefacto FR 29 (el tailoring).
//
//  El hallazgo lleva OrigenRegla.Responsable, que ClasificacionResponseParser
//  techa a OBS (es un desvío formal de completitud, no un incumplimiento de
//  fondo) y conserva su justificación verbatim.
//
//  FUNCIÓN PURA: sin I/O ni logging, para congelarla por golden output. La
//  lectura del campo vive en TailoringReader; acá solo se decide el hallazgo.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class HallazgosResponsable
{
    public static List<HallazgoPreliminar> Generar(DocumentosExtraidos input)
    {
        var hallazgos = new List<HallazgoPreliminar>();

        // Responsable presente → nada que reportar.
        if (!string.IsNullOrWhiteSpace(input.ResponsableTailoring))
            return hallazgos;

        // Responsable vacío: colgar OBS del artefacto FR 29 (el tailoring), si
        // está presente como exigible y aplicable (para que el consolidador, que
        // exige que el artefacto admita hallazgos, lo acepte).
        var fr29 = input.Artefactos.FirstOrDefault(a =>
            a.Exigibilidad == ExigibilidadArtefacto.Exigible
            && a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.Aplica
            && EsFr29(a.CodigoArtefacto));

        if (fr29 is null) return hallazgos;

        hallazgos.Add(new HallazgoPreliminar(
            ArtefactoEsperadoId: fr29.ArtefactoEsperadoId,
            Descripcion: "El tailoring del proyecto no tiene responsable asignado.",
            Justificacion:
                "La portada del tailoring (FR 29) tiene el campo 'Responsable' vacío. Ese campo " +
                "identifica quién definió el alcance del proyecto en el tailoring; dejarlo en " +
                "blanco es un desvío de completitud del tailoring. Se reporta como observación.",
            OrigenRegla: OrigenRegla.Responsable));

        return hallazgos;
    }

    /// <summary>
    /// True si el código corresponde al tailoring (FR 29), tolerante a separadores.
    /// El código del artefacto esperado es "FR 29" (sin sufijo de versión).
    /// </summary>
    private static bool EsFr29(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return false;

        var sb = new System.Text.StringBuilder(codigo.Length);
        foreach (var c in codigo)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c is '-' or '_' or '.') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString() == "fr29";
    }
}
