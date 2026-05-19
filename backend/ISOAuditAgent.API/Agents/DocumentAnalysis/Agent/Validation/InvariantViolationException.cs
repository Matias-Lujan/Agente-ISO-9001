namespace ISOAuditAgent.DocumentAnalysis.Agent.Validation;

public sealed class InvariantViolationException : Exception
{
    public InvariantViolationException(string message) : base(message)
    {
    }
}
