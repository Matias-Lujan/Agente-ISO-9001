namespace ISOAuditAgent.API.DTOs;

// ─── Input ───────────────────────────────────────────────────────────────────

public class ConsistencyVerificationRequest
{
    public required string AuditId { get; set; }
    public required string ProcessName { get; set; }
    public required List<DocumentContext> Documents { get; set; }
    public List<ValidationRule> ValidationRules { get; set; } = [];
}

public class DocumentContext
{
    public required string DocumentId { get; set; }
    public required string FileName { get; set; }
    public required string ContentText { get; set; }
    public string? Version { get; set; }
    public string? Author { get; set; }
    public DateTime? LastModified { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTime? ApprovalDate { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public DocumentType Type { get; set; }
}

public enum DocumentType { Policy, Procedure, Record, Form, Evidence, Other }

public class ValidationRule
{
    public required string RuleId { get; set; }
    public required string Description { get; set; }
    public RuleType Type { get; set; }
    public bool IsMandatory { get; set; } = true;
    public string? ExpectedValue { get; set; }
}

public enum RuleType
{
    RecordExists,
    DateSequence,
    SignatureRequired,
    VersionConsistency,
    CrossDocumentReference,
    Validity
}

// ─── Output ──────────────────────────────────────────────────────────────────

public class ConsistencyVerificationResult
{
    public required string AuditId { get; set; }
    public required string AgentName { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public AgentStatus Status { get; set; }
    public string? ErrorMessage { get; set; }

    public RecordValidationResult RecordValidation { get; set; } = new();
    public DateSignatureResult DateSignatureVerification { get; set; } = new();
    public CrossDocumentResult CrossDocumentConsistency { get; set; } = new();
    public ValidityResult ValidityCheck { get; set; } = new();

    public List<Finding> Findings { get; set; } = [];

    public int TotalChecks => Findings.Count;
    public int IssuesFound => Findings.Count(f => f.Severity != Severity.None);
}

public enum AgentStatus { Success, PartialSuccess, Failed }

// ── 4.4.1 Validar registros ──────────────────────────────────────────────────
public class RecordValidationResult
{
    public bool Completed { get; set; }
    public List<RecordCheck> Checks { get; set; } = [];
}

public class RecordCheck
{
    public required string RuleId { get; set; }
    public required string Description { get; set; }
    public bool Exists { get; set; }
    public bool IsComplete { get; set; }
    public string? Evidence { get; set; }
}

// ── 4.4.2 Verificar fechas/firmas ────────────────────────────────────────────
public class DateSignatureResult
{
    public bool Completed { get; set; }
    public List<DateSignatureCheck> Checks { get; set; } = [];
}

public class DateSignatureCheck
{
    public required string DocumentId { get; set; }
    public required string DocumentName { get; set; }
    public bool HasRequiredDates { get; set; }
    public bool DateSequenceIsValid { get; set; }
    public bool HasRequiredSignatures { get; set; }
    public string? Issue { get; set; }
}

// ── 4.4.3 Consistencia entre docs ────────────────────────────────────────────
public class CrossDocumentResult
{
    public bool Completed { get; set; }
    public List<ConsistencyCheck> Checks { get; set; } = [];
}

public class ConsistencyCheck
{
    public required string DocumentAId { get; set; }
    public required string DocumentBId { get; set; }
    public required string Aspect { get; set; }
    public bool IsConsistent { get; set; }
    public string? DiscrepancyDescription { get; set; }
}

// ── 4.4.4 Validar vigencia ───────────────────────────────────────────────────
public class ValidityResult
{
    public bool Completed { get; set; }
    public List<ValidityCheck> Checks { get; set; } = [];
}

public class ValidityCheck
{
    public required string DocumentId { get; set; }
    public required string DocumentName { get; set; }
    public bool IsValid { get; set; }
    public DateTime? ExpirationDate { get; set; }
    public int? DaysUntilExpiration { get; set; }
    public string? Issue { get; set; }
}

// ─── Finding ─────────────────────────────────────────────────────────────────
public class Finding
{
    public required string FindingId { get; set; }
    public required string Source { get; set; }
    public required string Description { get; set; }
    public Severity Severity { get; set; }
    public string? Evidence { get; set; }
    public string? RelatedDocumentId { get; set; }
    public string? RelatedRuleId { get; set; }
}

public enum Severity { None, Low, Medium, High, Critical }
