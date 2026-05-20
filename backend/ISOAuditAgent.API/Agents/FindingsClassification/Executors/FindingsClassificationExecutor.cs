using ISOAuditAgent.API.Agents.FindingsClassification.Agents;
using ISOAuditAgent.API.Agents.FindingsClassification.Contracts;
using ISOAuditAgent.API.Agents.FindingsClassification.Services;
using ISOAuditAgent.API.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;

namespace ISOAuditAgent.API.Agents.FindingsClassification.Executors;

/// <summary>
/// Orquesta la clasificación de hallazgos preliminares.
///
/// Dos modos de uso:
/// - HTTP (sin state): <see cref="ClasificarAsync"/> recibe la lista completa
///   de grupos (Compliance + Consistency) en un solo request y devuelve el
///   resultado clasificado.
/// - MAF (con state, fan-in barrier): <see cref="HandleAsync"/> acumula los
///   lotes a medida que llegan y emite el resultado cuando tiene los dos.
///
/// El acumulador interno vive por instancia. El servicio se registra como
/// <c>Scoped</c>, así cada request HTTP arranca con state limpio y cada
/// workflow MAF mantiene su acumulador propio.
/// </summary>
public class FindingsClassificationExecutor
{
    private readonly AIAgent _agent;
    private readonly ILogger<FindingsClassificationExecutor> _logger;
    private readonly List<HallazgosPreliminares> _acumulados = [];

    public FindingsClassificationExecutor(
        FindingsClassificationAgent agent,
        ILogger<FindingsClassificationExecutor> logger)
    {
        ArgumentNullException.ThrowIfNull(agent);
        _agent = agent.Agent;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Entry point para uso HTTP. Recibe la lista completa de grupos
    /// y devuelve el resultado clasificado en un solo llamado.
    /// </summary>
    public Task<HallazgosClasificados> ClasificarAsync(
        IReadOnlyList<HallazgosPreliminares> grupos,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(grupos);
        if (grupos.Count == 0)
            throw new ArgumentException(
                "Se requiere al menos un grupo de hallazgos.", nameof(grupos));

        return ClasificarInternoAsync(grupos, ct);
    }

    /// <summary>
    /// Entry point para uso MAF (workflow). Acumula los lotes y emite el
    /// resultado cuando se reciben los dos esperados.
    /// </summary>
    public async ValueTask HandleAsync(
        HallazgosPreliminares input,
        IWorkflowContext context)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(context);

        _acumulados.Add(input);
        if (_acumulados.Count < 2) return;

        var resultado = await ClasificarInternoAsync(_acumulados, CancellationToken.None);
        await context.YieldOutputAsync(resultado);
    }

    private async Task<HallazgosClasificados> ClasificarInternoAsync(
        IReadOnlyList<HallazgosPreliminares> grupos,
        CancellationToken ct)
    {
        var auditoriaId = grupos[0].AuditoriaId;

        // Diccionario ArtefactoEsperadoId → (HallazgoPreliminar, AgenteOrigen)
        // AgenteOrigen está en el raíz de HallazgosPreliminares, no en cada hallazgo.
        var origenPorArtefacto = grupos
            .SelectMany(g => g.Hallazgos.Select(h => (Hallazgo: h, Origen: g.AgenteOrigen)))
            .ToDictionary(x => x.Hallazgo.ArtefactoEsperadoId, x => (x.Hallazgo, x.Origen));

        var todos = origenPorArtefacto.Values
            .Select(x => x.Hallazgo)
            .ToList()
            .AsReadOnly();

        _logger.LogInformation(
            "FindingsClassification clasificando {Count} hallazgos (grupos: {Grupos}).",
            todos.Count, grupos.Count);

        var agentResponse = await _agent.RunAsync(ArmarPrompt(todos, auditoriaId));
        var respuestaGemini = agentResponse.Text ?? string.Empty;

        _logger.LogInformation("Gemini clasificó los hallazgos.");

        return ClasificacionResponseParser.Parsear(
            respuestaGemini, origenPorArtefacto, auditoriaId, _logger);
    }

    private static string ArmarPrompt(
        IReadOnlyList<HallazgoPreliminar> hallazgos,
        int auditoriaId)
    {
        var lineas = hallazgos.Select((h, i) =>
            $"{i + 1}. artefactoEsperadoId: {h.ArtefactoEsperadoId}\n" +
            $"   Descripcion: {h.Descripcion}\n" +
            $"   Justificacion: {h.Justificacion}\n" +
            $"   OrigenRegla: {h.OrigenRegla}"
        );

        return
            $"AuditoriaId: {auditoriaId}\n\n" +
            "Clasificá cada hallazgo en NC, OBS u OM según las reglas del PR 11-13.\n" +
            "Devolvé el mismo artefactoEsperadoId que recibiste.\n\n" +
            string.Join("\n\n", lineas) +
            "\n\nSOLO el array JSON. Sin texto adicional. Sin backticks.\n" +
            "[{ \"artefactoEsperadoId\": 1, \"tipo\": \"NC\", \"justificacion\": \"NC-02: ...\" }]";
    }

    public static string NombreEtapa(int id) => id switch
    {
        1 => "Planificación",
        2 => "Análisis y Diseño",
        3 => "Desarrollo",
        4 => "Testing",
        5 => "Seguimiento",
        6 => "Implementación",
        _ => "sin asignar"
    };
}
