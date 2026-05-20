using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;

namespace ISOAuditAgent.DocumentAnalysis.Agent;

public interface IDocumentAnalysisAgent
{
    Task<DocumentosExtraidos> ExecuteAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default);
}
