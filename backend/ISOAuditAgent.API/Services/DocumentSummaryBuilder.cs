using System.Text;
using ISOAuditAgent.API.DTOs;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Construye el resumen textual de los documentos
/// que se incluye en cada prompt que se manda a Gemini.
/// </summary>
public interface IDocumentSummaryBuilder
{
    string Build(IEnumerable<DocumentContext> documents);
    string BuildRulesList(IEnumerable<ValidationRule> rules);
    string BuildIssuesList(ConsistencyVerificationResult partial);
}

public class DocumentSummaryBuilder : IDocumentSummaryBuilder
{
    /// <summary>
    /// Genera un resumen legible de cada documento para incluir en el prompt.
    /// Limita el contenido a 2000 caracteres por documento para no 
    /// exceder el límite de tokens de Gemini.
    /// </summary>
    public string Build(IEnumerable<DocumentContext> documents)
    {
        var sb = new StringBuilder();

        foreach (var doc in documents)
        {
            sb.AppendLine($"--- DOCUMENTO: {doc.DocumentId} ---");
            sb.AppendLine($"Nombre: {doc.FileName}");
            sb.AppendLine($"Tipo: {doc.Type}");
            sb.AppendLine($"Versión: {doc.Version ?? "N/D"}");
            sb.AppendLine($"Autor: {doc.Author ?? "N/D"}");
            sb.AppendLine($"Última modificación: {doc.LastModified?.ToString("yyyy-MM-dd") ?? "N/D"}");
            sb.AppendLine($"Aprobado por: {doc.ApprovedBy ?? "N/D"}");
            sb.AppendLine($"Fecha aprobación: {doc.ApprovalDate?.ToString("yyyy-MM-dd") ?? "N/D"}");
            sb.AppendLine($"Vencimiento: {doc.ExpirationDate?.ToString("yyyy-MM-dd") ?? "Sin fecha de vencimiento"}");
            sb.AppendLine("Contenido:");

            // Limitamos el contenido para no exceder el contexto de la IA
            var preview = doc.ContentText.Length > 2000
                ? doc.ContentText[..2000] + "...[truncado]"
                : doc.ContentText;

            sb.AppendLine(preview);
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Convierte las reglas de validación a texto para el prompt.
    /// </summary>
    public string BuildRulesList(IEnumerable<ValidationRule> rules)
    {
        var sb = new StringBuilder();

        foreach (var rule in rules)
        {
            sb.AppendLine($"- [{rule.RuleId}] {rule.Description} | Tipo: {rule.Type} | Obligatorio: {rule.IsMandatory}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Consolida todos los problemas encontrados en las 4 sub-tareas
    /// para mandárselos a Gemini y que genere los hallazgos finales.
    /// </summary>
    public string BuildIssuesList(ConsistencyVerificationResult partial)
    {
        var sb = new StringBuilder();

        // Problemas de registros faltantes o incompletos
        foreach (var c in partial.RecordValidation.Checks.Where(x => !x.Exists || !x.IsComplete))
            sb.AppendLine($"[REGISTRO] {c.Description}: existe={c.Exists}, completo={c.IsComplete}");

        // Problemas de fechas y firmas
        foreach (var c in partial.DateSignatureVerification.Checks.Where(x => x.Issue != null))
            sb.AppendLine($"[FECHA/FIRMA] {c.DocumentName}: {c.Issue}");

        // Inconsistencias entre documentos
        foreach (var c in partial.CrossDocumentConsistency.Checks.Where(x => !x.IsConsistent))
            sb.AppendLine($"[CONSISTENCIA] {c.Aspect} entre {c.DocumentAId} y {c.DocumentBId}: {c.DiscrepancyDescription}");

        // Problemas de vigencia
        foreach (var c in partial.ValidityCheck.Checks.Where(x => !x.IsValid || (x.DaysUntilExpiration is < 30)))
            sb.AppendLine($"[VIGENCIA] {c.DocumentName}: {c.Issue ?? $"Vence en {c.DaysUntilExpiration} días"}");

        return sb.Length > 0 ? sb.ToString() : "No se detectaron problemas.";
    }
}