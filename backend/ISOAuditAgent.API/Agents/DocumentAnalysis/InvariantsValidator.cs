// ============================================================================
//  InvariantsValidator — DocumentAnalysis (Parte 4.C)
// ----------------------------------------------------------------------------
//  Validador de invariantes del DocumentosExtraidos (contrato 3) antes de
//  emitirlo al grafo. Su rol es defensa en profundidad: si el ArtefactosBuilder
//  fallara en algún edge case, este validador lo detecta antes de que el DTO
//  malformado se propague.
//
//  ORIGEN: Agent/Validation/InvariantsValidator.cs del diff, adaptado a:
//   - contrato 3 actual (EstadoAplicacionTailoring en lugar de EstadoTailoring).
//   - sin InvariantViolationException — uso InvalidOperationException con
//     mensaje. La excepción específica es ruido innecesario para Chat C.
//   - HashContenido SHA-256 hex 64 chars sigue intacto.
//
//  INVARIANTES VERIFICADAS (consistentes con contratos_agentes.md contrato 3):
//   - IDs (auditoría, proyecto, etapa, artefactoEsperado) > 0.
//   - NombreArtefacto no vacío.
//   - EtapaArtefactoId > 0.
//   - ArtefactoEsperadoId único entre los artefactos.
//   - SeccionesDetectadas nunca null (puede ser lista vacía).
//   - Encontrado ⇒ DocumentoEncontrado != null.
//   - Faltante / NoBuscado ⇒ DocumentoEncontrado == null.
//   - PendienteEtapaFutura ⇒ NoBuscado.
//   - Faltante ⇒ Exigibilidad = Exigible (no tiene sentido faltante sin ser
//     exigible).
//  RESTRICCIÓN MVP — Fuente == Drive:
//   El contrato 3 admite las 4 fuentes (Drive, Trello, Clockify, MSProject).
//   Esta restricción es una EXCEPCIÓN DEL AGENTE en el MVP, no del contrato:
//   el agente DocumentAnalysis del MVP solo verifica artefactos documentales
//   en Drive. Cuando se agreguen verificaciones para Trello/Clockify/MSProject
//   en post-MVP, esta validación debe relajarse junto con la del hash (porque
//   los registros de Trello/Clockify no producen un SHA-256 binario).
//   La línea está marcada con // MVP-DRIVE para localizarla.
//   - HashContenido es SHA-256 hex en minúsculas (64 chars).
//   - JustificacionNoAplica solo no-null cuando EstadoAplicacionTailoring =
//     NoAplica.
// ============================================================================

using System.Text.RegularExpressions;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

internal static partial class InvariantsValidator
{
    [GeneratedRegex("^[a-f0-9]{64}$", RegexOptions.CultureInvariant)]
    private static partial Regex HashHex64Regex();

    public static void Validar(DocumentosExtraidos dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        if (dto.AuditoriaId <= 0 || dto.ProyectoId <= 0 || dto.EtapaId <= 0)
        {
            throw new InvalidOperationException(
                "Identificadores de auditoría, proyecto o etapa deben ser > 0.");
        }

        if (dto.Artefactos is null)
        {
            throw new InvalidOperationException("Artefactos no puede ser null.");
        }

        var ids = new HashSet<int>();

        foreach (var a in dto.Artefactos)
        {
            if (a.ArtefactoEsperadoId <= 0)
            {
                throw new InvalidOperationException("ArtefactoEsperadoId debe ser > 0.");
            }

            if (string.IsNullOrWhiteSpace(a.NombreArtefacto))
            {
                throw new InvalidOperationException("NombreArtefacto no puede estar vacío.");
            }

            if (a.EtapaArtefactoId <= 0)
            {
                throw new InvalidOperationException("EtapaArtefactoId debe ser > 0.");
            }

            if (!ids.Add(a.ArtefactoEsperadoId))
            {
                throw new InvalidOperationException(
                    $"ArtefactoEsperadoId duplicado en la salida: {a.ArtefactoEsperadoId}.");
            }

            if (a.SeccionesDetectadas is null)
            {
                throw new InvalidOperationException(
                    "SeccionesDetectadas no puede ser null (usar lista vacía).");
            }

            if (a.SeccionesTemplate is null)
            {
                throw new InvalidOperationException(
                    "SeccionesTemplate no puede ser null (usar lista vacía).");
            }

            // Disponibilidad vs DocumentoEncontrado.
            if (a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado
                && a.DocumentoEncontrado is null)
            {
                throw new InvalidOperationException(
                    $"Artefacto {a.ArtefactoEsperadoId}: Encontrado requiere DocumentoEncontrado no-null.");
            }

            if (a.EstadoDisponibilidad is EstadoDisponibilidad.Faltante
                or EstadoDisponibilidad.NoBuscado
                && a.DocumentoEncontrado is not null)
            {
                throw new InvalidOperationException(
                    $"Artefacto {a.ArtefactoEsperadoId}: Faltante/NoBuscado requieren " +
                    "DocumentoEncontrado null.");
            }

            // Exigibilidad vs disponibilidad.
            if (a.Exigibilidad == ExigibilidadArtefacto.PendienteEtapaFutura
                && a.EstadoDisponibilidad != EstadoDisponibilidad.NoBuscado)
            {
                throw new InvalidOperationException(
                    $"Artefacto {a.ArtefactoEsperadoId}: PendienteEtapaFutura implica NoBuscado.");
            }

            if (a.EstadoDisponibilidad == EstadoDisponibilidad.Faltante
                && a.Exigibilidad != ExigibilidadArtefacto.Exigible)
            {
                throw new InvalidOperationException(
                    $"Artefacto {a.ArtefactoEsperadoId}: Faltante solo aplica con " +
                    "Exigibilidad = Exigible.");
            }

            // DocumentoEncontrado: invariantes propias.
            if (a.DocumentoEncontrado is not null)
            {
                // MVP-DRIVE: restricción del agente en el MVP, no del contrato 3.
                // El contrato admite las 4 fuentes; este agente solo valida
                // artefactos en Drive en esta versión. Relajar cuando se
                // sume soporte real para Trello/Clockify/MSProject.
                if (a.DocumentoEncontrado.Fuente == FuenteDocumento.MSProject)
                {
                    throw new InvalidOperationException(
                        $"Artefacto {a.ArtefactoEsperadoId}: MVP del agente DocumentAnalysis " +
                        $"soporta solo Fuente=Drive (recibido: {a.DocumentoEncontrado.Fuente}). " +
                        "Esta es una restricción del agente, no del contrato 3.");
                }

                // MVP-DRIVE: la validación de SHA-256 64 chars asume archivos
                // binarios. Si en post-MVP se permite Trello/Clockify, este
                // validador debe relajarse junto con el de Fuente.
                if (a.DocumentoEncontrado.Fuente == FuenteDocumento.Drive
                    && (string.IsNullOrEmpty(a.DocumentoEncontrado.HashContenido)
                        || !HashHex64Regex().IsMatch(a.DocumentoEncontrado.HashContenido)))
                {
                    throw new InvalidOperationException(
                        $"Artefacto {a.ArtefactoEsperadoId}: HashContenido debe ser " +
                        "SHA-256 hex en minúsculas (64 caracteres).");
                }
            }

            // Justificación solo en NoAplica.
            if (a.EstadoAplicacionTailoring != EstadoAplicacionTailoring.NoAplica
                && !string.IsNullOrEmpty(a.JustificacionNoAplica))
            {
                throw new InvalidOperationException(
                    $"Artefacto {a.ArtefactoEsperadoId}: JustificacionNoAplica debe " +
                    "ser null cuando EstadoAplicacionTailoring != NoAplica.");
            }
        }
    }
}
