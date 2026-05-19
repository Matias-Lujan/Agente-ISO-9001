using ISOAuditAgent.DocumentAnalysis.Agent.Models;

namespace ISOAuditAgent.DocumentAnalysis.Agent;

/// <summary>
/// Expone el contexto de auditoría precalculado (equivalente a la tool
/// <c>get_contexto_auditoria</c> del handoff).
/// </summary>
public interface IContextoAuditoriaService
{
    Task<ProyectoContexto> GetContextoAuditoriaAsync(
        int proyectoId,
        int etapaIdActual,
        CancellationToken cancellationToken = default);
}
