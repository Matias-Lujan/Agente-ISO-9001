// ============================================================================
//  HallazgosEstructurales — ConsistencyVerification (Fase 5)
// ----------------------------------------------------------------------------
//  Genera los hallazgos de estructura de documentos de forma determinística
//  en C#, sin pasar por el LLM, comparando las secciones del documento
//  encontrado contra las secciones del template del artefacto.
//
//  TRES CASOS DETERMINÍSTICOS:
//
//  1. Documento completamente vacío → NC / OrigenRegla.Procedimiento.
//     TODAS las secciones del documento (excluyendo la sección sintética
//     "(Contenido inicial)" del DocxTemplateParser) tienen TieneContenido=false.
//     Semánticamente equivale a "documento sin evidencia útil". OrigenRegla =
//     Procedimiento porque el procedimiento exige que el artefacto tenga
//     contenido real; NC/Procedimiento sobrevive el floor del ClasificacionParser
//     (línea 180: floor solo degrada NC de origen != Procedimiento).
//     Justificación generada completamente en C# (verbatim por la línea 135
//     de ClasificacionResponseParser: no la pisa el LLM).
//     Si se genera este NC, se omiten los OBS individuales del mismo artefacto
//     (el NC ya cubre la situación).
//
//  2. Sección del template ausente en el documento → OBS / OrigenRegla.Template.
//     Una sección definida en el template no aparece en SeccionesDetectadas
//     (matching por NormalizeHeaderKey, tolerante a tildes y mayúsculas).
//     Solo se evalúa cuando el documento NO es completamente vacío.
//
//  3. Sección del template presente en el documento pero vacía → OBS / Template.
//     La sección matchea por NormalizeHeaderKey pero TieneContenido = false.
//     Solo se evalúa cuando el documento NO es completamente vacío.
//
//  CRITERIOS DE FILTRADO:
//  - Solo artefactos con SeccionesTemplate no vacío (template disponible y
//    parseado con éxito). Sin template → el LLM maneja la verificación.
//  - La sección sintética "(Contenido inicial)" se excluye del diff (es
//    un artefacto del DocxTemplateParser, no una sección real del template).
//  - Matching entre títulos de secciones: NormalizeHeaderKey (lowercase,
//    sin tildes, whitespace colapsado) desde TailoringColumnMapper.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class HallazgosEstructurales
{
    private const string SeccionSintetica = "(Contenido inicial)";

    /// <summary>
    /// Genera hallazgos de estructura para los artefactos que tienen template.
    /// Recibe solo los artefactos Exigibles + Encontrados + con SeccionesTemplate.
    /// </summary>
    public static List<HallazgoPreliminar> Generar(
        IReadOnlyList<ArtefactoExtraido> artefactos,
        string procedimientoCodigo,
        string procedimientoNombre)
    {
        var hallazgos = new List<HallazgoPreliminar>();

        foreach (var a in artefactos)
        {
            if (a.SeccionesTemplate.Count == 0) continue;

            var seccionesDoc = a.SeccionesDetectadas
                .Where(s => s.Titulo != SeccionSintetica)
                .ToList();

            // Dedup por clave normalizada: el parser puede emitir la misma
            // sección dos veces (ej. como heading Y como campo de tabla)
            // cuando el template tiene ambas estructuras con el mismo título.
            var seccionesTemplate = a.SeccionesTemplate
                .Where(s => s.Titulo != SeccionSintetica)
                .DistinctBy(s => TailoringColumnMapper.NormalizeHeaderKey(s.Titulo))
                .ToList();

            // Caso 1: documento completamente vacío → NC/Procedimiento.
            // Se evalúa antes que los OBS: si aplica, los OBS individuales
            // serían redundantes.
            if (seccionesDoc.Count > 0 && seccionesDoc.All(s => !s.TieneContenido))
            {
                var nombreDoc = a.DocumentoEncontrado?.NombreArchivo ?? a.NombreArtefacto;
                hallazgos.Add(new HallazgoPreliminar(
                    ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                    Descripcion:
                        $"Artefacto '{a.NombreArtefacto}' encontrado pero completamente vacío.",
                    Justificacion:
                        $"El documento '{nombreDoc}' asociado al artefacto " +
                        $"'{a.NombreArtefacto}' " +
                        (a.CodigoArtefacto is not null ? $"({a.CodigoArtefacto}) " : string.Empty) +
                        $"fue encontrado, pero todas sus secciones (excluyendo la sección " +
                        $"sintética de cabecera) están vacías. Un documento sin ningún " +
                        $"contenido real no constituye evidencia válida del artefacto. " +
                        $"El procedimiento {procedimientoCodigo} ({procedimientoNombre}) " +
                        $"exige que los artefactos documentados cuenten con contenido sustantivo.",
                    OrigenRegla: OrigenRegla.Procedimiento));
                // NC cubre el artefacto completo — no se generan OBS individuales.
                continue;
            }

            // Casos 2 y 3: sección ausente o vacía en documento sustantivo.
            foreach (var secTemplate in seccionesTemplate)
            {
                var keyTemplate = TailoringColumnMapper.NormalizeHeaderKey(secTemplate.Titulo);
                var secDoc = seccionesDoc.FirstOrDefault(sd =>
                    TailoringColumnMapper.NormalizeHeaderKey(sd.Titulo) == keyTemplate);

                if (secDoc is null)
                {
                    // Caso 2: sección del template ausente en el documento.
                    hallazgos.Add(new HallazgoPreliminar(
                        ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                        Descripcion:
                            $"Sección '{secTemplate.Titulo}' requerida por el template " +
                            $"no está presente en el documento.",
                        Justificacion:
                            $"El template del artefacto '{a.NombreArtefacto}' define la " +
                            $"sección '{secTemplate.Titulo}', pero esta sección no se " +
                            $"encontró en el documento al comparar por título (tolerante " +
                            $"a tildes y mayúsculas). La ausencia de una sección requerida " +
                            $"puede indicar que el documento no sigue la estructura acordada " +
                            $"en el template del {procedimientoCodigo}.",
                        OrigenRegla: OrigenRegla.Template));
                }
                else if (!secDoc.TieneContenido)
                {
                    // Caso 3: sección del template presente pero vacía.
                    hallazgos.Add(new HallazgoPreliminar(
                        ArtefactoEsperadoId: a.ArtefactoEsperadoId,
                        Descripcion:
                            $"Sección '{secDoc.Titulo}' del artefacto '{a.NombreArtefacto}' " +
                            $"está vacía.",
                        Justificacion:
                            $"La sección '{secDoc.Titulo}' está presente en el documento " +
                            $"del artefacto '{a.NombreArtefacto}', pero no contiene " +
                            $"ningún texto. Según el template del {procedimientoCodigo}, " +
                            $"esta sección debe tener contenido.",
                        OrigenRegla: OrigenRegla.Template));
                }
            }
        }

        return hallazgos;
    }
}
