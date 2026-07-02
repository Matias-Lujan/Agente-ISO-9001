// ============================================================================
//  Interfaces de repositorio del WORKFLOW del orchestrator (portadas de dev).
// ----------------------------------------------------------------------------
//  Estas interfaces NO existían en esta rama; las consume el motor de
//  auditoría (ResolutorContextoService, AuditoriaPersistenceService, runner,
//  worker). Las interfaces compartidas (IAuditoriaRepository, IHallazgoRepository,
//  IProyectoRepository, IProcedimientoRepository) viven en sus archivos propios
//  y se les agregaron los métodos del motor por separado.
// ============================================================================

using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Acceso a la tabla key-value ConfiguracionSistema (ej. 'path_carpeta_templates').
/// </summary>
public interface IConfiguracionRepository
{
    Task<string?> ObtenerValorOrNullAsync(string clave, CancellationToken ct);
}

/// <summary>
/// Inserción de ArtefactoEvaluado (primera tabla del resultado: genera los IDs
/// con los que se resuelven las FKs de Hallazgo y DocumentoAnalizado).
/// </summary>
public interface IArtefactoEvaluadoRepository
{
    Task<int> AgregarAsync(ArtefactoEvaluado artefactoEvaluado, CancellationToken ct);
}

/// <summary>
/// Inserción de un lote de DocumentoAnalizado (FK artefacto_evaluado_id ya resuelta).
/// </summary>
public interface IDocumentoAnalizadoRepository
{
    Task AgregarRangoAsync(IEnumerable<DocumentoAnalizado> documentos, CancellationToken ct);
}

/// <summary>
/// Abstracción mínima de transacción sobre el DbContext. La usa
/// AuditoriaPersistenceService para envolver las inserciones del resultado
/// en una sola unidad atómica.
/// </summary>
public interface IUnitOfWork
{
    Task EjecutarEnTransaccionAsync(Func<CancellationToken, Task> operacion, CancellationToken ct);
}

/// <summary>
/// Lectura de la tabla auditoria_progreso. La escritura la hace
/// IAuditoriaProgresoTracker.
/// </summary>
public interface IAuditoriaProgresoRepository
{
    Task<IReadOnlyList<AuditoriaProgreso>> ObtenerPorAuditoriaAsync(
        int auditoriaId, CancellationToken ct);
}
