using System.Text.RegularExpressions;
using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Validation;

public static class InvariantsValidator
{
    private static readonly Regex Hex64 = new(
        @"^[a-f0-9]{64}$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public static void Validate(DocumentosExtraidos dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.AuditoriaId <= 0 || dto.ProyectoId <= 0 || dto.EtapaId <= 0)
            throw new InvariantViolationException(
                "Identificadores de auditoría, proyecto o etapa deben ser > 0.");

        if (dto.Artefactos is null)
            throw new InvariantViolationException("Artefactos no puede ser null.");

        var seen = new HashSet<int>();
        foreach (var a in dto.Artefactos)
        {
            if (a.ArtefactoEsperadoId <= 0)
                throw new InvariantViolationException("ArtefactoEsperadoId debe ser > 0.");

            if (string.IsNullOrWhiteSpace(a.NombreArtefacto))
                throw new InvariantViolationException("NombreArtefacto no puede estar vacío.");

            if (a.EtapaArtefactoId <= 0)
                throw new InvariantViolationException("EtapaArtefactoId debe ser > 0.");

            if (!seen.Add(a.ArtefactoEsperadoId))
                throw new InvariantViolationException(
                    $"ArtefactoEsperadoId duplicado: {a.ArtefactoEsperadoId}.");

            if (a.SeccionesDetectadas is null)
                throw new InvariantViolationException("SeccionesDetectadas no puede ser null.");

            if (a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado && a.DocumentoEncontrado is null)
                throw new InvariantViolationException(
                    "Encontrado requiere DocumentoEncontrado no null.");

            if (a.EstadoDisponibilidad is EstadoDisponibilidad.Faltante or EstadoDisponibilidad.NoBuscado
                && a.DocumentoEncontrado is not null)
                throw new InvariantViolationException(
                    "Faltante/NoBuscado requieren DocumentoEncontrado null.");

            if (a.Exigibilidad == ExigibilidadArtefacto.PendienteEtapaFutura)
            {
                if (a.EstadoDisponibilidad != EstadoDisponibilidad.NoBuscado || a.DocumentoEncontrado is not null)
                    throw new InvariantViolationException(
                        "PendienteEtapaFutura implica NoBuscado y DocumentoEncontrado null.");
            }

            if (a.EstadoDisponibilidad == EstadoDisponibilidad.Faltante
                && a.Exigibilidad != ExigibilidadArtefacto.Exigible)
                throw new InvariantViolationException(
                    "Faltante solo aplica con Exigibilidad Exigible.");

            if (a.DocumentoEncontrado is not null)
            {
                if (a.DocumentoEncontrado.Fuente != FuenteDocumento.Drive)
                    throw new InvariantViolationException("MVP: Fuente debe ser Drive.");

                if (string.IsNullOrEmpty(a.DocumentoEncontrado.HashContenido)
                    || !Hex64.IsMatch(a.DocumentoEncontrado.HashContenido))
                {
                    throw new InvariantViolationException(
                        "HashContenido debe ser SHA-256 hex en minúsculas (64 caracteres).");
                }
            }

            if (a.EstadoTailoring != EstadoTailoring.NoAplica
                && !string.IsNullOrEmpty(a.JustificacionNoAplica))
            {
                throw new InvariantViolationException(
                    "JustificacionNoAplica debe ser null cuando EstadoTailoring != NoAplica.");
            }
        }
    }
}
