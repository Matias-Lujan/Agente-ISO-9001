
using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;
using ISOAuditAgent.API.Services;
using Microsoft.Agents.AI;

namespace ISOAuditAgent.API.Agents.Orchestrator;

public static class OrchestratorServiceCollectionExtensions
{
    public static IServiceCollection AddAuditoriaOrchestrator(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // --- A. Persistencia ---
        services.AddScoped<IAuditoriaPersistenceService, AuditoriaPersistenceService>();

        // --- B. Nodos (6, Scoped) ---

        // Determinista: solo ResolutorContextoService como dependencia.
        services.AddScoped<ResolutorContextoNode>();

        // LLM con I/O async. Resuelve AIAgent keyed + ITailoringSource +
        // IArtefactoFisicoChecker (todos ya registrados por Chat D / D4).
        services.AddScoped<DocumentAnalysisNode>(sp => new DocumentAnalysisNode(
            sp.GetRequiredKeyedService<AIAgent>("DocumentAnalysis"),
            sp.GetRequiredService<ITailoringSource>(),
            sp.GetRequiredService<IArtefactoFisicoChecker>(),
            sp.GetRequiredService<TrelloChecker>(),
            sp.GetRequiredService<ClockifyChecker>(),
            sp.GetRequiredService<IAuditoriaProgresoTracker>(),
            sp.GetRequiredService<IRegistroErrorAuditoriaWriter>(),
            sp.GetRequiredService<ILogger<DocumentAnalysisNode>>()));

        // LLM puro: AIAgent keyed + tracker de progreso + writer de errores.
        services.AddScoped<ComplianceValidationNode>(sp => new ComplianceValidationNode(
            sp.GetRequiredKeyedService<AIAgent>("ComplianceValidation"),
            sp.GetRequiredService<IAuditoriaProgresoTracker>(),
            sp.GetRequiredService<IRegistroErrorAuditoriaWriter>(),
            sp.GetRequiredService<ILogger<ComplianceValidationNode>>()));

        services.AddScoped<ConsistencyVerificationNode>(sp => new ConsistencyVerificationNode(
            sp.GetRequiredKeyedService<AIAgent>("ConsistencyVerification"),
            sp.GetRequiredService<IAuditoriaProgresoTracker>(),
            sp.GetRequiredService<IRegistroErrorAuditoriaWriter>(),
            sp.GetRequiredService<ILogger<ConsistencyVerificationNode>>()));

        services.AddScoped<FindingsClassificationNode>(sp => new FindingsClassificationNode(
            sp.GetRequiredKeyedService<AIAgent>("FindingsClassification"),
            sp.GetRequiredService<IAuditoriaProgresoTracker>(),
            sp.GetRequiredService<IRegistroErrorAuditoriaWriter>(),
            sp.GetRequiredService<ILogger<FindingsClassificationNode>>()));

        // Determinista, sin dependencias.
        services.AddScoped<ConsolidadorResultadoNode>();

        // --- C. Cola + Runner (Singleton) ---
        services.AddSingleton<IAuditoriaQueue, AuditoriaQueue>();
        services.AddSingleton<AuditoriaRunner>();

        // --- D. Worker (Hosted Service, Singleton implícito) ---
        services.AddHostedService<AuditoriaWorkerService>();

        return services;
    }
}