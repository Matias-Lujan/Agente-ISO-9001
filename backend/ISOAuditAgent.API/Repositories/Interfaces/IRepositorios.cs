// ============================================================================
//  Interfaces de repositorio — Agente Inteligente de Auditoría ISO 9001
// ----------------------------------------------------------------------------
//  Estas interfaces son el puente entre la BD (EF Core / MySQL) y dos
//  consumidores distintos:
//
//   1. ResolutorContexto  — nodo determinista al inicio del workflow.
//      Usa los repositorios de LECTURA para armar el DTO ContextoAuditoria
//      que consume DocumentAnalysis. Los agentes NO tocan estos repositorios:
//      acceden a datos externos solo vía MCP (ERS 4.3, RNF-03).
//
//   2. API REST (vía AuditoriaPersistenceService) — usa los repositorios de
//      ESCRITURA para persistir el AuditoriaResultado en transacción única
//      (contratos_agentes.md, contrato 5).
//
//  Convenciones:
//   - Todo async, con CancellationToken.
//   - Lectura devuelve entidades EF, no DTOs. Los DTOs del workflow los
//     arma el nodo correspondiente, no el repositorio.
//   - Nullable explícito: ...OrNullAsync cuando la fila puede no existir.
//   - Los repositorios NO abren transacciones ni saben de "exigibilidad".
//     Esa lógica vive en el workflow / service.
// ============================================================================

using ISOAuditAgent.API.Models;          // entidades EF: Proyecto, Etapa, etc.

namespace ISOAuditAgent.API.Repositories;

// ----------------------------------------------------------------------------
//  LECTURA — consumidos por ResolutorContexto
// ----------------------------------------------------------------------------

/// <summary>
/// Acceso de lectura al Proyecto. ResolutorContexto lo usa para obtener los
/// IDs de integración (drive/trello/clockify), el tipo (A/B) y el
/// procedimiento que rige al proyecto.
/// </summary>
public interface IProyectoRepository
{
    /// <summary>
    /// Trae el proyecto por id. Devuelve null si no existe.
    /// La entidad incluye: tipo_proyecto, horas_estimadas, procedimiento_id,
    /// drive_folder_id, trello_board_id, clockify_project_id (estos tres
    /// nullables — el proyecto puede no usar todas las herramientas).
    /// </summary>
    Task<Proyecto?> ObtenerPorIdOrNullAsync(int proyectoId, CancellationToken ct);
}

/// <summary>
/// Acceso de lectura a la estructura de un procedimiento: sus etapas y los
/// artefactos esperados en cada una.
/// </summary>
public interface IProcedimientoRepository
{
    /// <summary>
    /// Trae todas las etapas de un procedimiento, ordenadas por el campo
    /// 'orden'. ResolutorContexto las usa para ubicar la etapa auditada en la
    /// secuencia y derivar exigibilidad (anteriores + actual = exigible).
    /// </summary>
    Task<IReadOnlyList<Etapa>> ObtenerEtapasAsync(int procedimientoId, CancellationToken ct);

    /// <summary>
    /// Trae TODOS los ArtefactoEsperado del procedimiento, cada uno con su
    /// Etapa cargada (Include). El repositorio devuelve el conjunto completo
    /// sin filtrar por exigibilidad: la clasificación exigible / futuro la
    /// hace ResolutorContexto comparando Etapa.orden. El repo no conoce el
    /// concepto de "exigible".
    /// </summary>
    Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosEsperadosAsync(
        int procedimientoId, CancellationToken ct);
}

/// <summary>
/// Acceso a la tabla key-value ConfiguracionSistema. En el MVP se usa para
/// resolver 'path_carpeta_templates' (consultas_cliente.md, consulta 1).
/// </summary>
public interface IConfiguracionRepository
{
    /// <summary>
    /// Devuelve el valor de una clave de configuración, o null si no existe.
    /// </summary>
    Task<string?> ObtenerValorOrNullAsync(string clave, CancellationToken ct);
}

// ----------------------------------------------------------------------------
//  AUDITORÍA — lectura + cambios de estado
// ----------------------------------------------------------------------------

/// <summary>
/// Acceso a la entidad Auditoria. La API la crea en EnCurso ANTES de encolar
/// el trabajo; el background worker actualiza el estado al terminar.
/// La persistencia del RESULTADO completo (artefactos, hallazgos, documentos)
/// NO vive acá — la coordina AuditoriaPersistenceService. Este repositorio
/// solo maneja la entidad Auditoria en sí.
/// </summary>
public interface IAuditoriaRepository
{
    /// <summary>
    /// Crea una auditoría en estado EnCurso. Devuelve el id generado.
    /// La llama la API REST en el POST /api/auditorias, antes de encolar.
    /// </summary>
    Task<int> CrearEnCursoAsync(
        int proyectoId, int usuarioId, int etapaId, CancellationToken ct);

    /// <summary>
    /// Trae la auditoría por id (para el polling del frontend y para el
    /// worker). Null si no existe.
    /// </summary>
    Task<Auditoria?> ObtenerPorIdOrNullAsync(int auditoriaId, CancellationToken ct);

    /// <summary>
    /// Marca la auditoría como Completada y setea fecha_finalizacion_utc.
    /// La llama AuditoriaPersistenceService DENTRO de la transacción de
    /// persistencia del resultado.
    /// </summary>
    Task MarcarCompletadaAsync(int auditoriaId, CancellationToken ct);

    /// <summary>
    /// Marca la auditoría como Fallida y setea fecha_finalizacion_utc.
    /// La llama el worker si el workflow lanza excepción o si la
    /// transacción de persistencia hace rollback.
    /// </summary>
    Task MarcarFallidaAsync(int auditoriaId, CancellationToken ct);
}

// ----------------------------------------------------------------------------
//  ESCRITURA DEL RESULTADO — repositorios finos
// ----------------------------------------------------------------------------
//  Estos tres repositorios solo agregan filas. NO abren transacción: el
//  AuditoriaPersistenceService abre una sola transacción EF Core y coordina
//  las tres inserciones + el cambio de estado de la Auditoria. Si algo falla,
//  rollback completo (contratos_agentes.md, contrato 5).
// ----------------------------------------------------------------------------

/// <summary>
/// Inserción de ArtefactoEvaluado. Es la PRIMERA tabla que se persiste:
/// genera los IDs con los que después se resuelven las FKs de Hallazgo y
/// DocumentoAnalizado.
/// </summary>
public interface IArtefactoEvaluadoRepository
{
    /// <summary>
    /// Inserta una fila de ArtefactoEvaluado y devuelve el id generado.
    /// Pensado para llamarse dentro de un bucle bajo una transacción abierta
    /// por el service. El service usa los ids devueltos para construir el
    /// mapa ArtefactoEsperadoId -> ArtefactoEvaluadoId.
    /// </summary>
    Task<int> AgregarAsync(ArtefactoEvaluado artefactoEvaluado, CancellationToken ct);
}

/// <summary>
/// Inserción de Hallazgo. Se persiste DESPUÉS de ArtefactoEvaluado porque su
/// FK artefacto_evaluado_id depende de los ids ya generados.
/// </summary>
public interface IHallazgoRepository
{
    /// <summary>
    /// Inserta un lote de hallazgos. El service ya resolvió la FK
    /// artefacto_evaluado_id de cada uno con el mapa.
    /// </summary>
    Task AgregarRangoAsync(IEnumerable<Hallazgo> hallazgos, CancellationToken ct);
}

/// <summary>
/// Inserción de DocumentoAnalizado. Se persiste DESPUÉS de ArtefactoEvaluado
/// por la misma razón que Hallazgo (FK artefacto_evaluado_id).
/// </summary>
public interface IDocumentoAnalizadoRepository
{
    /// <summary>
    /// Inserta un lote de documentos analizados. El service ya resolvió la FK
    /// artefacto_evaluado_id de cada uno con el mapa.
    /// </summary>
    Task AgregarRangoAsync(IEnumerable<DocumentoAnalizado> documentos, CancellationToken ct);
}

// ----------------------------------------------------------------------------
//  UNIT OF WORK — el dueño de la transacción
// ----------------------------------------------------------------------------

/// <summary>
/// Abstracción mínima de transacción sobre el DbContext de EF Core. La usa
/// AuditoriaPersistenceService para envolver las inserciones del resultado
/// (ArtefactoEvaluado + Hallazgo + DocumentoAnalizado + cambio de estado de
/// Auditoria) en una sola unidad atómica.
///
/// Nota: con EF Core el SaveChanges de un solo DbContext ya es atómico. El
/// IUnitOfWork explícito hace falta acá porque la persistencia se hace en
/// PASOS (insertar artefactos -> obtener ids -> insertar hallazgos), y entre
/// pasos hay SaveChanges intermedios para materializar los ids. El
/// BeginTransaction agrupa todos esos SaveChanges en un commit único.
/// </summary>
public interface IUnitOfWork
{
    Task EjecutarEnTransaccionAsync(Func<CancellationToken, Task> operacion, CancellationToken ct);
}
