// ============================================================================
//  AuditoriaQueue — Cola en memoria de auditorías pendientes
// ----------------------------------------------------------------------------
//  El workflow de auditoría es asíncrono respecto al request HTTP: una
//  auditoría ejecuta 4 agentes LLM + accesos MCP y puede tardar minutos, más
//  que un timeout HTTP razonable.
//
//  Patrón: la API encola el AuditoriaId y responde 202 Accepted; un
//  IHostedService (AuditoriaWorkerService) consume la cola y procesa.
//
//  Implementación: System.Threading.Channels — cola productor/consumidor
//  en proceso, sin infraestructura externa (sin broker). Suficiente para el
//  MVP. Si en el futuro se necesita durabilidad de la cola (sobrevivir a un
//  reinicio) o varios procesos worker, se reemplaza por una cola real
//  (RabbitMQ, Azure Service Bus) sin tocar a los consumidores: solo cambia
//  la implementación de IAuditoriaQueue.
//
//  Nota: si el proceso se reinicia con auditorías encoladas sin procesar,
//  esas auditorías quedan en estado EnCurso en la BD. Recuperarlas (volver a
//  encolar las EnCurso al arrancar, o marcarlas Fallida) es una mejora
//  posible, fuera del alcance del MVP. Queda registrada como decisión.
// ============================================================================

using System.Threading.Channels;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Cola de auditorías pendientes de ejecución. La API escribe; el worker lee.
/// </summary>
public interface IAuditoriaQueue
{
    /// <summary>
    /// Encola una auditoría ya creada en BD (estado EnCurso) para que el
    /// worker la procese. La llama la API REST tras el POST /api/auditorias.
    /// </summary>
    ValueTask EncolarAsync(int auditoriaId, CancellationToken ct = default);

    /// <summary>
    /// Espera y devuelve el próximo AuditoriaId pendiente. La consume el
    /// worker en su loop. Se bloquea (asincrónicamente) si la cola está vacía.
    /// </summary>
    ValueTask<int> TomarAsync(CancellationToken ct);
}

/// <summary>
/// Implementación de IAuditoriaQueue sobre un Channel ilimitado.
/// Se registra como Singleton: una sola cola para todo el proceso.
/// </summary>
public sealed class AuditoriaQueue : IAuditoriaQueue
{
    private readonly Channel<int> _channel;

    public AuditoriaQueue()
    {
        // Canal ilimitado: encolar nunca bloquea. El volumen de auditorías de
        // este sistema es bajo (acción manual de un auditor), así que no hace
        // falta poner un límite ni back-pressure.
        var opciones = new UnboundedChannelOptions
        {
            SingleReader = true,   // un solo worker consume
            SingleWriter = false   // varios requests de la API pueden encolar
        };
        _channel = Channel.CreateUnbounded<int>(opciones);
    }

    public ValueTask EncolarAsync(int auditoriaId, CancellationToken ct = default)
        => _channel.Writer.WriteAsync(auditoriaId, ct);

    public ValueTask<int> TomarAsync(CancellationToken ct)
        => _channel.Reader.ReadAsync(ct);
}
