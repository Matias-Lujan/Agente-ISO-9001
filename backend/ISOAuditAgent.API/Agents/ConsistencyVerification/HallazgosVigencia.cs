// ============================================================================
//  HallazgosVigencia — ConsistencyVerification
// ----------------------------------------------------------------------------
//  Valida, de forma determinística (sin LLM), que el formulario usado en cada
//  documento esté en su versión vigente, comparando la VigenciaDetectada en el
//  documento contra la VigenciaEsperada de la referencia de Calidad.
//
//  REGLA (acordada con el cliente):
//   Por cada artefacto Exigible + Encontrado:
//    - Sin referencia de Calidad (VigenciaEsperada == null) → no se valida.
//      (ResolutorContexto ya logueó el warning.)
//    - Formato no parseable (DocumentoParseable == false)   → no se valida.
//      (ArtefactoFisicoChecker ya logueó el warning.)
//    - Parseable pero sin Vigencia detectada                → OBS.
//    - VigenciaDetectada != VigenciaEsperada                → OBS.
//    - VigenciaDetectada == VigenciaEsperada                → conforme (sin hallazgo).
//
//  Todos los hallazgos llevan OrigenRegla.Vigencia, que ClasificacionResponseParser
//  techa a OBS (usar un formulario desactualizado es un desvío formal, no un
//  incumplimiento de fondo).
//
//  FUNCIÓN PURA: sin logging ni I/O, para poder congelarla por golden output.
//  Los warnings de "sin referencia" / "formato no soportado" viven en la capa que
//  los conoce (ResolutorContexto y el checker, respectivamente).
// ============================================================================

using System.Globalization;
using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class HallazgosVigencia
{
    public static List<HallazgoPreliminar> Generar(IReadOnlyList<ArtefactoExtraido> artefactos)
    {
        var hallazgos = new List<HallazgoPreliminar>();

        foreach (var a in artefactos)
        {
            // Solo sobre artefactos exigibles efectivamente encontrados.
            if (a.Exigibilidad != ExigibilidadArtefacto.Exigible) continue;
            if (a.EstadoDisponibilidad != EstadoDisponibilidad.Encontrado) continue;

            // Sin referencia de Calidad para este FR → no se valida (ya logueado).
            if (a.VigenciaEsperada is not { } esperada) continue;

            // Formato no parseable → no se valida (ya logueado en el checker).
            if (!a.DocumentoParseable) continue;

            var refArtefacto = a.CodigoArtefacto is not null
                ? $"{a.CodigoArtefacto} '{a.NombreArtefacto}'"
                : $"'{a.NombreArtefacto}'";

            // Parseable pero sin vigencia detectada en el documento → OBS.
            if (a.VigenciaDetectada is not { } detectada)
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion:
                        $"No se detectó la fecha de vigencia en el documento de {refArtefacto}.",
                    Justificacion:
                        $"El documento del artefacto {refArtefacto} no declara una fecha de " +
                        $"vigencia detectable, por lo que no puede verificarse que use la versión " +
                        $"vigente del formulario según Calidad (vigente: {Formatear(esperada)}). " +
                        $"Se reporta como observación para revisión manual del formulario usado.",
                    OrigenRegla: OrigenRegla.Vigencia));
                continue;
            }

            // Vigencia distinta de la vigente → formulario desactualizado → OBS.
            if (detectada != esperada)
            {
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion:
                        $"El formulario de {refArtefacto} no usa la versión vigente según Calidad.",
                    Justificacion:
                        $"El documento de {refArtefacto} declara vigencia {Formatear(detectada)}, " +
                        $"pero la versión vigente del formulario según el Departamento de Calidad es " +
                        $"{Formatear(esperada)}. Usar una versión desactualizada del formulario es un " +
                        $"desvío formal respecto del estándar vigente.",
                    OrigenRegla: OrigenRegla.Vigencia));
            }
        }

        return hallazgos;
    }

    private static string Formatear(DateOnly fecha) =>
        fecha.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);
}
