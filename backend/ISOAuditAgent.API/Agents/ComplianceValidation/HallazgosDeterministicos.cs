// ============================================================================
//  HallazgosDeterministicos — ComplianceValidation (Parte 4.D)
// ----------------------------------------------------------------------------
//  Genera los hallazgos que el contrato 4 línea 275 obliga a producir de forma
//  determinística, SIN pasar por el LLM. Esa decisión (D2) se tomó porque son
//  chequeos sobre el DTO de entrada: no requieren razonamiento.
//
//  TRES CASOS DETERMINÍSTICOS — REGLAS INDEPENDIENTES:
//
//  Las tres reglas se evalúan por separado sobre cada artefacto. Pueden
//  coexistir (mismo artefacto, dos hallazgos), aunque por la lógica actual
//  del ArtefactosBuilder de 4.C no ocurra en la práctica. El contrato 4
//  línea 307 garantiza que la regla 1-a-1 es por HallazgoPreliminar, no por
//  artefacto, y FindingsClassification (4.A) está preparado para hallazgos
//  múltiples sobre un mismo artefacto. Evaluar las reglas como independientes
//  es más robusto que asumir exclusión mutua.
//
//  1. Artefacto exigible + EstadoAplicacionTailoring = Aplica + EstadoDisponibilidad = Faltante.
//     El tailoring lo declara aplicable, el sistema fue a buscarlo y no estaba.
//     Es violación directa del PR 11-13. Verificamos Aplica EXPLÍCITAMENTE
//     (no nos apoyamos en la cadena de invariantes del builder): si el builder
//     cambiara en el futuro, este chequeo se autodefiende.
//
//  2. EstadoAplicacionTailoring = NoAplica SIN justificación.
//     El cliente fue explícito (consultas_cliente.md consulta 5): "si es
//     No(justificar) debería estar sí o sí la justificación".
//
//  3. EstadoAplicacionTailoring = SinDeclararEnTailoring.
//     El PR 11-13 requiere que TODO artefacto del procedimiento esté declarado
//     en el FR-29, con su decisión (Aplica = Sí o No con justificación). No
//     declararlo es un incumplimiento.
//
//  TODOS los tres casos tienen OrigenRegla = Procedimiento. Esto es crítico:
//  FindingsClassification aplica la regla de oro "OrigenRegla != Procedimiento
//  → como mucho OM". Como los tres vienen del procedimiento, no se degradan
//  automáticamente — pueden clasificarse como NC.
//
//  FILTRADO INICIAL — INVARIANTE LÍNEA 268:
//  Los hallazgos se generan SOLO sobre artefactos exigibles. Los
//  PendienteEtapaFutura no producen hallazgos: los descartamos al inicio del
//  foreach para que ninguna de las 3 reglas los considere.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

internal static class HallazgosDeterministicos
{
    /// <summary>
    /// Genera los hallazgos determinísticos del contrato 4 línea 275 a partir
    /// del DocumentosExtraidos de entrada.
    /// </summary>
    public static List<HallazgoPreliminar> Generar(DocumentosExtraidos input)
    {
        var hallazgos = new List<HallazgoPreliminar>();

        foreach (var a in input.Artefactos)
        {
            // Invariante línea 268: solo sobre artefactos exigibles.
            if (a.Exigibilidad != ExigibilidadArtefacto.Exigible)
            {
                continue;
            }

            // Las 3 reglas se evalúan independientemente. Si dos coexistieran
            // (hoy no pasa por la lógica del builder de 4.C, pero podría con
            // cambios futuros), ambas generan su hallazgo. 1-a-1 por hallazgo,
            // no por artefacto (contrato 4 línea 307).

            // Regla 1: Aplica + Faltante.
            // Verificación EXPLÍCITA de Aplica: si el builder cambiara y emitiera
            // Faltante con otra combinación, esta regla no se dispararía con una
            // justificación falsa.
            if (a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.Aplica
                && a.EstadoDisponibilidad == EstadoDisponibilidad.Faltante)
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto exigible '{a.NombreArtefacto}' no se encontró.",
                    Justificacion:
                        "El artefacto pertenece a la etapa actual o a una etapa anterior " +
                        "y por lo tanto es exigible. El sistema intentó verificar su " +
                        "existencia física pero no encontró evidencia asociada. El PR 11-13 " +
                        "exige que los artefactos definidos como exigibles estén disponibles " +
                        "o queden debidamente justificados en el tailoring.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }

            // Regla 2: NoAplica sin justificación.
            if (a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.NoAplica
                && string.IsNullOrWhiteSpace(a.JustificacionNoAplica))
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto '{a.NombreArtefacto}' marcado como 'No aplica' sin justificación en el tailoring.",
                    Justificacion:
                        "El cliente confirmó que cuando el tailoring marca un artefacto " +
                        "como 'No (justificar)' debe existir una justificación textual. " +
                        "Sin esa justificación, la exclusión no es válida.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }

            // Regla 3: SinDeclararEnTailoring.
            if (a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.SinDeclararEnTailoring)
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto '{a.NombreArtefacto}' no figura en el tailoring del proyecto.",
                    Justificacion:
                        "El PR 11-13 establece que la obligatoriedad de los artefactos " +
                        "del proyecto se define en el FR-29 Tailoring del Proyecto. " +
                        "Este artefacto forma parte del procedimiento esperado pero no " +
                        "fue declarado en el tailoring con una decisión explícita.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }
        }

        return hallazgos;
    }
}
