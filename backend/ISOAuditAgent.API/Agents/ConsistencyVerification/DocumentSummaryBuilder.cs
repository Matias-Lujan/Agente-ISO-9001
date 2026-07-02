// ============================================================================
//  DocumentSummaryBuilder — ConsistencyVerification (Parte 4.B)
// ----------------------------------------------------------------------------
//  Convierte una lista de ArtefactoExtraido en texto plano legible por el LLM.
//  El LLM no entiende objetos C# — necesita texto estructurado.
//
//  ORIGEN: Services/DocumentSummaryBuilder.cs del diff de consistency.
//
//  CAMBIOS RESPECTO DEL DIFF:
//   - Movido a la carpeta del agente — es detalle interno del agente, no
//     servicio compartido. Si otro agente lo necesitara, se promueve.
//   - Eliminada la interfaz IDocumentSummaryBuilder. No hay implementaciones
//     alternativas y no se mockea (input puro). Static class alcanza.
//   - Sin filtrado propio: asume que la lista recibida YA viene filtrada por
//     el llamador. El filtrado es responsabilidad del nodo, vive una sola
//     vez en ConstruirPrompt, no aquí. (Defensa en profundidad redundante
//     en el diff: filtra en el service y otra vez en el builder.)
// ============================================================================

using System.Text;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

internal static class DocumentSummaryBuilder
{
    /// <summary>
    /// Arma el texto-resumen de los artefactos para inyectar en el prompt del LLM.
    /// Asume que <paramref name="artefactos"/> ya viene filtrada a los analizables
    /// (Exigibles + Encontrados + con al menos una sección vacía); no filtra de nuevo.
    ///
    /// Para artefactos con template: muestra el estado de cada sección del template
    /// en el documento (LLENA / VACIA), de modo que el LLM tenga contexto para
    /// razonar si una sección vacía es realmente un hallazgo.
    /// Para artefactos sin template: muestra todas las secciones detectadas con su estado.
    /// </summary>
    public static string Construir(IReadOnlyList<ArtefactoExtraido> artefactos)
    {
        if (artefactos.Count == 0)
            return "No hay artefactos encontrados y exigibles para analizar.";

        var sb = new StringBuilder();

        foreach (var a in artefactos)
        {
            sb.AppendLine($"--- ARTEFACTO ID: {a.ArtefactoEsperadoId} ---");
            sb.AppendLine($"Nombre         : {a.NombreArtefacto}");
            sb.AppendLine($"Codigo         : {a.CodigoArtefacto ?? "N/A"}");
            sb.AppendLine($"Obligatoriedad : {a.Obligatoriedad}");
            sb.AppendLine($"Archivo        : {a.DocumentoEncontrado?.NombreArchivo ?? "N/A"}");
            // Propósito del artefacto según el procedimiento: es el criterio para
            // juzgar si una sección vacía compromete para qué sirve el documento.
            if (!string.IsNullOrWhiteSpace(a.Descripcion))
                sb.AppendLine($"Proposito      : {a.Descripcion}");

            if (a.SeccionesTemplate.Count > 0)
            {
                // Con template: mostrar el estado de cada sección del template en
                // el documento. El LLM necesita ver qué secciones el procedimiento
                // definió como parte del artefacto para razonar sobre su vacuidad.
                // El documento puede repetir una sección que normaliza a la misma
                // clave (ej. "Probabilidad" en una matriz de riesgos). ToDictionary
                // explotaría con "clave duplicada"; agrupamos y nos quedamos con una
                // ocurrencia LLENA si existe — basta una llena para considerar la
                // sección presente y con contenido.
                var docPorClave = a.SeccionesDetectadas
                    .GroupBy(s => TailoringColumnMapper.NormalizeHeaderKey(s.Titulo))
                    .ToDictionary(
                        g => g.Key,
                        g => g.FirstOrDefault(s => s.TieneContenido) ?? g.First());

                sb.AppendLine("Secciones del template (estado en el documento):");
                foreach (var st in a.SeccionesTemplate)
                {
                    var clave = TailoringColumnMapper.NormalizeHeaderKey(st.Titulo);
                    if (!docPorClave.TryGetValue(clave, out var secDoc))
                        continue; // ausente → hallazgo determinístico, ya generado

                    var estado = secDoc.TieneContenido ? "LLENA" : "VACIA";
                    sb.AppendLine($"  - {st.Titulo}: {estado}");
                }
            }
            else
            {
                sb.AppendLine("Secciones detectadas (sin template formal):");
                foreach (var s in a.SeccionesDetectadas)
                {
                    var estado = s.TieneContenido ? "LLENA" : "VACIA";
                    sb.AppendLine($"  - {s.Titulo}: {estado}");
                }
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
