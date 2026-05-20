using System.Text;
using ISOAuditAgent.API.Agents.ConsistencyVerification.Contracts;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Convierte el DocumentosExtraidos (Contrato 2) en texto legible
/// para incluirlo en el prompt que le mandamos a Gemini.
/// Gemini no entiende objetos C# — necesita texto plano bien estructurado.
/// </summary>
public interface IDocumentSummaryBuilder
{
    string BuildArtefactosSummary(IReadOnlyList<ArtefactoExtraido> artefactos);
}

public class DocumentSummaryBuilder : IDocumentSummaryBuilder
{
    /// <summary>
    /// Genera el resumen de artefactos para el prompt de Gemini.
    /// Solo incluye artefactos Exigibles y Encontrados.
    /// Los demas no tienen sentido analizar: si no esta, no hay nada que verificar.
    /// No incluimos fechas ni firmas porque el cliente aclaro que no se validan.
    /// </summary>
    public string BuildArtefactosSummary(IReadOnlyList<ArtefactoExtraido> artefactos)
    {
        var sb = new StringBuilder();

        var analizables = artefactos
            .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible
                     && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado)
            .ToList();

        if (analizables.Count == 0)
            return "No hay artefactos encontrados y exigibles para analizar.";

        foreach (var a in analizables)
        {
            sb.AppendLine($"--- ARTEFACTO ID: {a.ArtefactoEsperadoId} ---");
            sb.AppendLine($"Nombre         : {a.NombreArtefacto}");
            sb.AppendLine($"Codigo         : {a.CodigoArtefacto ?? "N/A"}");
            sb.AppendLine($"Obligatoriedad : {a.Obligatoriedad}");
            sb.AppendLine($"Archivo        : {a.DocumentoEncontrado?.NombreArchivo ?? "N/A"}");
            sb.AppendLine($"Fuente         : {a.DocumentoEncontrado?.Fuente}");

            if (a.SeccionesDetectadas.Count > 0)
            {
                sb.AppendLine("Secciones detectadas:");
                foreach (var s in a.SeccionesDetectadas)
                {
                    // TieneContenido = false significa que la seccion existe pero esta vacia
                    var estado = s.TieneContenido ? "con contenido" : "VACIA";
                    sb.AppendLine($"  - {s.Titulo}: {estado}");
                }
            }
            else
            {
                sb.AppendLine("Secciones: no aplica template para este tipo de artefacto");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }
}
