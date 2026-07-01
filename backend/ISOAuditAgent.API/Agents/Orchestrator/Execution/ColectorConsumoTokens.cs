using System.Collections.Concurrent;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Un registro de consumo de una única llamada al LLM: qué agente la hizo, con
/// qué modelo, y cuántos tokens reportó Gemini (entrada / salida / total).
/// </summary>
public readonly record struct RegistroConsumo(
    string AgenteKey,
    string Modelo,
    long TokensEntrada,
    long TokensSalida,
    long TokensTotal);

/// <summary>
/// Acumulador en memoria del consumo de tokens de una auditoría. Se registra
/// Scoped: hay uno por scope de auditoría (el que crea AuditoriaRunner), y lo
/// comparten los 4 AIAgent de ese scope a través de ChatClientConMetricas.
///
/// Debe ser thread-safe porque los nodos ComplianceValidation y
/// ConsistencyVerification corren en paralelo (fan-out del workflow) y ambos
/// registran consumo sobre la misma instancia. De ahí el ConcurrentQueue.
///
/// El AuditoriaRunner drena esta cola al terminar la auditoría y persiste las
/// filas estampándoles el AuditoriaId (que el colector no conoce).
/// </summary>
public sealed class ColectorConsumoTokens
{
    private readonly ConcurrentQueue<RegistroConsumo> _registros = new();

    /// <summary>Registra el consumo de una llamada al LLM.</summary>
    public void Registrar(RegistroConsumo registro) => _registros.Enqueue(registro);

    /// <summary>
    /// Vacía la cola y devuelve todo lo acumulado. Idempotente: una segunda
    /// llamada devuelve vacío.
    /// </summary>
    public IReadOnlyList<RegistroConsumo> Drenar()
    {
        var lista = new List<RegistroConsumo>();
        while (_registros.TryDequeue(out var r))
            lista.Add(r);
        return lista;
    }
}
