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
                var justReg1 = string.IsNullOrWhiteSpace(a.UrlReferencia)
                    ? $"El tailoring del proyecto declara '{a.NombreArtefacto}' como 'Aplica', " +
                      $"comprometiendo su existencia para la etapa auditada. " +
                      $"El sistema buscó la evidencia y no la encontró. " +
                      $"El procedimiento {input.ProcedimientoCodigo} ({input.ProcedimientoNombre}) " +
                      $"exige que todo artefacto aplicable esté disponible o quede justificado " +
                      $"en el tailoring; un artefacto comprometido y ausente sin justificación " +
                      $"es una no conformidad directa."
                    : $"El tailoring del proyecto declara '{a.NombreArtefacto}' como 'Aplica', " +
                      $"comprometiendo su existencia para la etapa auditada. " +
                      $"El tailoring declara una URL de referencia para este artefacto, " +
                      $"pero el sistema buscó la evidencia y no la encontró. " +
                      $"El procedimiento {input.ProcedimientoCodigo} ({input.ProcedimientoNombre}) " +
                      $"exige que todo artefacto aplicable esté disponible o quede justificado " +
                      $"en el tailoring; un artefacto comprometido y ausente sin justificación " +
                      $"es una no conformidad directa.";

                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto exigible '{a.NombreArtefacto}' no se encontró.",
                    Justificacion: justReg1,
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
                        $"El tailoring del proyecto declara '{a.NombreArtefacto}' como 'No aplica', " +
                        $"pero la justificación de esa exclusión está vacía. " +
                        $"La política de tailoring acordada con el cliente exige que toda exclusión " +
                        $"de un artefacto incluya una justificación textual que la sustente. " +
                        $"Sin ella, la exclusión no puede validarse contra el procedimiento " +
                        $"{input.ProcedimientoCodigo} ({input.ProcedimientoNombre}) " +
                        $"y constituye una no conformidad.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }

            // Regla 4: Aplica + ExportFallido.
            // El archivo existe en Drive pero no pudo exportarse (ej: formato nativo
            // Google con tamaño >10 MB). No es lo mismo que Faltante: el archivo
            // está, pero el sistema no pudo verificar su contenido.
            if (a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.Aplica
                && a.EstadoDisponibilidad == EstadoDisponibilidad.ExportFallido)
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto '{a.NombreArtefacto}' encontrado en Drive pero no procesable (export fallido).",
                    Justificacion:
                        $"El sistema encontró un archivo asociado al artefacto '{a.NombreArtefacto}' " +
                        $"en Google Drive, pero no pudo exportar su contenido para verificación. " +
                        $"El archivo está almacenado en formato nativo de Google (Google Docs/Sheets/Slides) " +
                        $"y probablemente supera el límite de 10 MB impuesto por la API de export de Drive, " +
                        $"o la service account no tiene permisos suficientes sobre ese archivo. " +
                        $"Sin acceso al contenido no es posible verificar la conformidad del artefacto " +
                        $"con los requisitos del procedimiento {input.ProcedimientoCodigo} ({input.ProcedimientoNombre}). " +
                        $"Se requiere revisión manual o convertir el archivo a formato estándar " +
                        $"(.docx / .xlsx / .pdf) en el Drive del proyecto.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }

            // Regla 3: SinDeclararEnTailoring.
            if (a.EstadoAplicacionTailoring == EstadoAplicacionTailoring.SinDeclararEnTailoring)
            {
                var artefactoRef = a.CodigoArtefacto is not null
                    ? $"{a.CodigoArtefacto} '{a.NombreArtefacto}'"
                    : $"'{a.NombreArtefacto}'";
                var mandatorio = a.Obligatoriedad == ObligatoriedadArtefacto.Mandatorio
                    ? " y mandatorio"
                    : string.Empty;

                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion: $"Artefacto '{a.NombreArtefacto}' no figura en el tailoring del proyecto.",
                    Justificacion:
                        $"El procedimiento {input.ProcedimientoCodigo} ({input.ProcedimientoNombre}) " +
                        $"exige que cada artefacto esperado tenga en el tailoring del proyecto " +
                        $"una decisión explícita de aplicabilidad ('Aplica' o 'No aplica' con justificación). " +
                        $"El tailoring no contiene ninguna entrada para {artefactoRef}, " +
                        $"artefacto exigible{mandatorio} en la etapa auditada. " +
                        $"Al no haber decisión declarada, no puede determinarse si el artefacto " +
                        $"debía producirse: la omisión es una no conformidad con el requisito " +
                        $"de completitud del tailoring.",
                    OrigenRegla: OrigenRegla.Procedimiento));
            }
        }

        return hallazgos;
    }
}
