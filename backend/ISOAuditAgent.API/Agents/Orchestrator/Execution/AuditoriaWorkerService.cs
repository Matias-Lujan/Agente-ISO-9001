// ============================================================================
//  AuditoriaWorkerService — Worker en background que procesa auditorías
// ----------------------------------------------------------------------------
//  IHostedService (BackgroundService) que corre durante toda la vida del
//  proceso. Es una CÁSCARA FINA: su única responsabilidad es el loop
//
//      tomar un AuditoriaId de la cola  ->  delegar en el AuditoriaRunner
//
//  Toda la lógica de ejecución (scope, workflow, persistencia, manejo de
//  errores) vive en AuditoriaRunner. Esta separación deja el runner testeable
//  sin levantar el host.
//
//  Procesamiento secuencial: el worker procesa una auditoría por vez. Es
//  suficiente para el MVP (las auditorías son acciones manuales de un auditor,
//  no hay alto volumen) y evita problemas de concurrencia sobre Gemini free
//  tier (rate limits) y sobre la BD. Si en el futuro se necesita paralelismo,
//  se procesa el canal con varias tareas concurrentes — sin tocar el runner.
//
//  Relación con el ciclo de vida del host:
//   - ExecuteAsync corre hasta que el host se apaga (stoppingToken se cancela).
//   - Una auditoría en curso al momento del apagado: ver nota en el catch de
//     OperationCanceledException.
// ============================================================================

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
