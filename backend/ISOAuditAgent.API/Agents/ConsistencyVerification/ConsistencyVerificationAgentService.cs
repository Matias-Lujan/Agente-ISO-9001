using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Services;
using Microsoft.Extensions.Logging;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

/// <summary>
/// Agente 4.4 - ConsistencyVerification
/// 
/// Orquesta las 4 sub-tareas del WBS:
///   4.4.1 → ValidateRecordsAsync          (Validar registros)
///   4.4.2 → VerifyDatesAndSignaturesAsync  (Verificar fechas/firmas)
///   4.4.3 → CheckCrossDocumentAsync        (Consistencia entre docs)
///   4.4.4 → ValidateValidityAsync          (Validar vigencia)
/// </summary>
public class ConsistencyVerificationAgentService
{
    private readonly IAiClient _ai;
    private readonly IDocumentSummaryBuilder _summaryBuilder;
    private readonly ILogger<ConsistencyVerificationAgentService> _logger;

    public ConsistencyVerificationAgentService(
        IAiClient ai,
        IDocumentSummaryBuilder summaryBuilder,
        ILogger<ConsistencyVerificationAgentService> logger)
    {
        _ai = ai;
        _summaryBuilder = summaryBuilder;
        _logger = logger;
    }

    // ── Entry point (llamado por el orquestador) ──────────────────────────────

    public async Task<ConsistencyVerificationResult> ExecuteAsync(
        ConsistencyVerificationRequest request,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ConsistencyVerification iniciado | AuditId={AuditId} | Proceso={Process} | Docs={Count}",
            request.AuditId, request.ProcessName, request.Documents.Count);

        var result = new ConsistencyVerificationResult
        {
            AuditId = request.AuditId,
            AgentName = "ConsistencyVerification",
            Status = AgentStatus.Success
        };

        var docSummary = _summaryBuilder.Build(request.Documents);

        try
        {
            // Las 4 sub-tareas se ejecutan en secuencia
            result.RecordValidation = await ValidateRecordsAsync(
                request.ProcessName, request.ValidationRules, docSummary, ct);

            result.DateSignatureVerification = await VerifyDatesAndSignaturesAsync(
                request.ProcessName, docSummary, ct);

            result.CrossDocumentConsistency = await CheckCrossDocumentAsync(
                request.ProcessName, docSummary, ct);

            result.ValidityCheck = await ValidateValidityAsync(
                request.ProcessName, docSummary, ct);

            // Consolidar todos los hallazgos al final
            result.Findings = await ConsolidateFindingsAsync(
                request.ProcessName, result, ct);

            _logger.LogInformation(
                "ConsistencyVerification completado | AuditId={AuditId} | Hallazgos={Count}",
                request.AuditId, result.Findings.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ConsistencyVerification falló | AuditId={AuditId}", request.AuditId);
            result.Status = AgentStatus.Failed;
            result.ErrorMessage = ex.Message;
        }

        return result;
    }

    // ── 4.4.1 Validar registros ───────────────────────────────────────────────

    private async Task<RecordValidationResult> ValidateRecordsAsync(
        string processName,
        List<ValidationRule> rules,
        string docSummary,
        CancellationToken ct)
    {
        _logger.LogDebug("4.4.1 ValidateRecords iniciado");

        var rulesText = _summaryBuilder.BuildRulesList(rules);
        var prompt = ConsistencyPrompts.RecordValidation(processName, rulesText, docSummary);

        var rawResponse = await _ai.CompleteAsync(
            systemPrompt: "Eres un auditor ISO 9001 experto. Responde siempre en JSON válido.",
            userMessage: prompt,
            ct);

        var parsed = AiResponseParser.ParseJson<RecordValidationAiResponse>(rawResponse, _logger);

        var result = new RecordValidationResult { Completed = parsed != null };

        if (parsed?.Checks != null)
        {
            result.Checks = parsed.Checks.Select(c => new RecordCheck
            {
                RuleId = c.RuleId ?? "N/D",
                Description = c.Description ?? "N/D",
                Exists = c.Exists,
                IsComplete = c.IsComplete,
                Evidence = c.Evidence
            }).ToList();
        }

        _logger.LogDebug("4.4.1 ValidateRecords: {Count} verificaciones", result.Checks.Count);
        return result;
    }

    // ── 4.4.2 Verificar fechas y firmas ──────────────────────────────────────

    private async Task<DateSignatureResult> VerifyDatesAndSignaturesAsync(
        string processName,
        string docSummary,
        CancellationToken ct)
    {
        _logger.LogDebug("4.4.2 VerifyDatesAndSignatures iniciado");

        var prompt = ConsistencyPrompts.DateSignatureVerification(processName, docSummary);

        var rawResponse = await _ai.CompleteAsync(
            systemPrompt: "Eres un auditor ISO 9001 experto. Responde siempre en JSON válido.",
            userMessage: prompt,
            ct);

        var parsed = AiResponseParser.ParseJson<DateSignatureAiResponse>(rawResponse, _logger);

        var result = new DateSignatureResult { Completed = parsed != null };

        if (parsed?.Checks != null)
        {
            result.Checks = parsed.Checks.Select(c => new DateSignatureCheck
            {
                DocumentId = c.DocumentId ?? "N/D",
                DocumentName = c.DocumentName ?? "N/D",
                HasRequiredDates = c.HasRequiredDates,
                DateSequenceIsValid = c.DateSequenceIsValid,
                HasRequiredSignatures = c.HasRequiredSignatures,
                Issue = c.Issue
            }).ToList();
        }

        _logger.LogDebug("4.4.2 VerifyDatesAndSignatures: {Count} verificaciones", result.Checks.Count);
        return result;
    }

    // ── 4.4.3 Consistencia entre documentos ──────────────────────────────────

    private async Task<CrossDocumentResult> CheckCrossDocumentAsync(
        string processName,
        string docSummary,
        CancellationToken ct)
    {
        _logger.LogDebug("4.4.3 CrossDocumentConsistency iniciado");

        var prompt = ConsistencyPrompts.CrossDocumentConsistency(processName, docSummary);

        var rawResponse = await _ai.CompleteAsync(
            systemPrompt: "Eres un auditor ISO 9001 experto. Responde siempre en JSON válido.",
            userMessage: prompt,
            ct);

        var parsed = AiResponseParser.ParseJson<CrossDocumentAiResponse>(rawResponse, _logger);

        var result = new CrossDocumentResult { Completed = parsed != null };

        if (parsed?.Checks != null)
        {
            result.Checks = parsed.Checks.Select(c => new ConsistencyCheck
            {
                DocumentAId = c.DocumentAId ?? "N/D",
                DocumentBId = c.DocumentBId ?? "N/D",
                Aspect = c.Aspect ?? "N/D",
                IsConsistent = c.IsConsistent,
                DiscrepancyDescription = c.DiscrepancyDescription
            }).ToList();
        }

        _logger.LogDebug("4.4.3 CrossDocumentConsistency: {Count} verificaciones", result.Checks.Count);
        return result;
    }

    // ── 4.4.4 Validar vigencia ────────────────────────────────────────────────

    private async Task<ValidityResult> ValidateValidityAsync(
        string processName,
        string docSummary,
        CancellationToken ct)
    {
        _logger.LogDebug("4.4.4 ValidateValidity iniciado");

        var currentDate = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var prompt = ConsistencyPrompts.ValidityCheck(processName, docSummary, currentDate);

        var rawResponse = await _ai.CompleteAsync(
            systemPrompt: "Eres un auditor ISO 9001 experto. Responde siempre en JSON válido.",
            userMessage: prompt,
            ct);

        var parsed = AiResponseParser.ParseJson<ValidityAiResponse>(rawResponse, _logger);

        var result = new ValidityResult { Completed = parsed != null };

        if (parsed?.Checks != null)
        {
            result.Checks = parsed.Checks.Select(c => new ValidityCheck
            {
                DocumentId = c.DocumentId ?? "N/D",
                DocumentName = c.DocumentName ?? "N/D",
                IsValid = c.IsValid,
                ExpirationDate = c.ExpirationDate,
                DaysUntilExpiration = c.DaysUntilExpiration,
                Issue = c.Issue
            }).ToList();
        }

        _logger.LogDebug("4.4.4 ValidateValidity: {Count} verificaciones", result.Checks.Count);
        return result;
    }

    // ── Consolidación de hallazgos ────────────────────────────────────────────

    private async Task<List<Finding>> ConsolidateFindingsAsync(
        string processName,
        ConsistencyVerificationResult partial,
        CancellationToken ct)
    {
        _logger.LogDebug("Consolidando hallazgos");

        var issuesText = _summaryBuilder.BuildIssuesList(partial);

        // Si no hay problemas, no llamamos a Gemini
        if (issuesText == "No se detectaron problemas.")
            return [];

        var prompt = ConsistencyPrompts.FindingsConsolidation(processName, issuesText);

        var rawResponse = await _ai.CompleteAsync(
            systemPrompt: "Eres un auditor ISO 9001 experto. Responde siempre en JSON válido.",
            userMessage: prompt,
            ct);

        var parsed = AiResponseParser.ParseJson<FindingsAiResponse>(rawResponse, _logger);

        if (parsed?.Findings == null) return [];

        return parsed.Findings.Select((f, i) => new Finding
        {
            FindingId = $"CV-{partial.AuditId}-{i + 1:000}",
            Source = f.Source ?? "ConsistencyVerification",
            Description = f.Description ?? "Sin descripción",
            Severity = Enum.TryParse<Severity>(f.Severity, true, out var sev) ? sev : Severity.Medium,
            Evidence = f.Evidence
        }).ToList();
    }

    // ── Tipos internos para deserializar respuestas de Gemini ─────────────────

    private record RecordValidationAiResponse(List<RecordCheckAi>? Checks, string? Summary);
    private record RecordCheckAi(string? RuleId, string? Description, bool Exists, bool IsComplete, string? Evidence);

    private record DateSignatureAiResponse(List<DateSignatureCheckAi>? Checks);
    private record DateSignatureCheckAi(
        string? DocumentId, string? DocumentName,
        bool HasRequiredDates, bool DateSequenceIsValid,
        bool HasRequiredSignatures, string? Issue);

    private record CrossDocumentAiResponse(List<CrossDocumentCheckAi>? Checks);
    private record CrossDocumentCheckAi(
        string? DocumentAId, string? DocumentBId,
        string? Aspect, bool IsConsistent, string? DiscrepancyDescription);

    private record ValidityAiResponse(List<ValidityCheckAi>? Checks);
    private record ValidityCheckAi(
        string? DocumentId, string? DocumentName,
        bool IsValid, DateTime? ExpirationDate,
        int? DaysUntilExpiration, string? Issue);

    private record FindingsAiResponse(List<FindingAi>? Findings);
    private record FindingAi(string? Description, string? Severity, string? Evidence, string? Source);
}