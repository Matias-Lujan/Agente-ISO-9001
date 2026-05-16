using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using AgenteAuditoria.Models;
using AgenteAuditoria.Services;

namespace AgenteAuditoria.Executors;

public class FindingsClassificationExecutor(AIAgent agent)
{
    private readonly List<HallazgosPreliminares> _acumulados = [];

    public async ValueTask HandleAsync(
        HallazgosPreliminares input,
        IWorkflowContext context)
    {
        _acumulados.Add(input);
        if (_acumulados.Count < 2) return;
        var resultado = await ClasificarAsync(_acumulados);
        await context.YieldOutputAsync(resultado);
    }

    public async Task<HallazgosClasificados?> ProcesarAsync(HallazgosPreliminares input)
    {
        _acumulados.Add(input);
        if (_acumulados.Count < 2) return null;
        return await ClasificarAsync(_acumulados);
    }

    private async Task<HallazgosClasificados> ClasificarAsync(
        List<HallazgosPreliminares> grupos)
    {
        var auditoriaId = grupos.First().AuditoriaId;

        // Construye diccionario ArtefactoEsperadoId → (HallazgoPreliminar, AgenteOrigen)
        // AgenteOrigen está en el raíz de HallazgosPreliminares, no en cada hallazgo.
        var origenPorArtefacto = grupos
            .SelectMany(g => g.Hallazgos.Select(h => (Hallazgo: h, Origen: g.AgenteOrigen)))
            .ToDictionary(x => x.Hallazgo.ArtefactoEsperadoId, x => (x.Hallazgo, x.Origen));

        var todos = origenPorArtefacto.Values
            .Select(x => x.Hallazgo)
            .ToList()
            .AsReadOnly();

        Console.WriteLine(
            $"[FindingsClassification] Clasificando {todos.Count} hallazgos " +
            $"({grupos[0].AgenteOrigen}: {grupos[0].Hallazgos.Count} + " +
            $"{grupos[1].AgenteOrigen}: {grupos[1].Hallazgos.Count})...");

        var agentResponse   = await agent.RunAsync(ArmarPrompt(todos, auditoriaId));
        var respuestaGemini = agentResponse.Text ?? string.Empty;

        Console.WriteLine("[FindingsClassification] ✓ Gemini clasificó los hallazgos.");

        return ClasificacionResponseParser.Parsear(
            respuestaGemini, origenPorArtefacto, auditoriaId);
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