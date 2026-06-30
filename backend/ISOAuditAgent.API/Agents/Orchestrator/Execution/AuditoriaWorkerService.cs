
namespace ISOAuditAgent.API.Agents.Orchestrator;

public sealed class AuditoriaWorkerService : BackgroundService
{
    private readonly IAuditoriaQueue _cola;
    private readonly AuditoriaRunner _runner;
    private readonly ILogger<AuditoriaWorkerService> _logger;

    public AuditoriaWorkerService(
        IAuditoriaQueue cola,
        AuditoriaRunner runner,
        ILogger<AuditoriaWorkerService> logger)
    {
        _cola = cola;
        _runner = runner;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AuditoriaWorkerService iniciado.");

        while (!stoppingToken.IsCancellationRequested)
        {
            int auditoriaId;

            try
            {
                // Espera (asincrónica) hasta que haya una auditoría encolada.
                // Si el host se apaga, TomarAsync lanza OperationCanceledException.
                auditoriaId = await _cola.TomarAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Apagado normal del host mientras la cola estaba vacía.
                // No es un error: se sale del loop.
                break;
            }

            // EjecutarAsync no lanza: traduce cualquier error a estado Fallida.
            // Aun así se envuelve en try/catch como última red de seguridad —
            // un fallo procesando UNA auditoría no debe tumbar el worker, que
            // tiene que seguir disponible para las siguientes.
            try
            {
                await _runner.EjecutarAsync(auditoriaId, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // El host se apagó mientras una auditoría estaba en curso.
                // La auditoría queda en EnCurso en la BD (el runner no llegó a
                // marcar Completada/Fallida). Recuperar las EnCurso al próximo
                // arranque es una mejora fuera del alcance del MVP — queda
                // registrada como decisión, igual que en AuditoriaQueue.
                _logger.LogWarning(
                    "Worker cancelado mientras procesaba la auditoría " +
                    "{AuditoriaId}. Puede haber quedado en EnCurso.",
                    auditoriaId);
                break;
            }
            catch (Exception ex)
            {
                // No debería ocurrir: EjecutarAsync ya maneja sus errores.
                // Si llega acá, es un fallo inesperado del propio runner. Se
                // loguea y el worker SIGUE con la próxima auditoría.
                _logger.LogError(ex,
                    "Fallo inesperado procesando la auditoría {AuditoriaId}. " +
                    "El worker continúa con las siguientes.",
                    auditoriaId);
            }
        }

        _logger.LogInformation("AuditoriaWorkerService detenido.");
    }
}
