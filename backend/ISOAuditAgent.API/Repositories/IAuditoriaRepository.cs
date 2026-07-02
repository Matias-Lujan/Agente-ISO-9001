using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de auditorias.
/// Define que operaciones existen sin decir como se implementan.
/// </summary>
public interface IAuditoriaRepository
{
    /// <summary>
    /// Devuelve todas las auditorias activas.
    /// </summary>
    Task<IReadOnlyList<Auditoria>> ObtenerTodasAsync();

    /// <summary>
    /// Devuelve las auditorias de un proyecto especifico.
    /// </summary>
    Task<IReadOnlyList<Auditoria>> ObtenerPorProyectoAsync(int proyectoId);

    /// <summary>
    /// Busca una auditoria por su ID.
    /// Devuelve null si no existe.
    /// </summary>
    Task<Auditoria?> ObtenerPorIdAsync(int id);

    /// <summary>
    /// Crea una auditoria nueva con estado "en_curso".
    /// </summary>
    Task<Auditoria> CrearAsync(Auditoria auditoria);

    /// <summary>
    /// Actualiza el estado y fecha de finalizacion de una auditoria.
    /// Se usa cuando el workflow de agentes termina (completada o fallida).
    /// </summary>
    Task<Auditoria> ActualizarAsync(Auditoria auditoria);

    // ── Usados por el workflow del orchestrator (portado de dev) ──────────────

    /// <summary>
    /// Crea una auditoría en estado EnCurso y devuelve el id generado.
    /// La llama el POST /api/auditorias antes de encolar el trabajo.
    /// </summary>
    Task<int> CrearEnCursoAsync(int proyectoId, int usuarioId, int etapaId, CancellationToken ct);

    /// <summary>
    /// Trae la auditoría por id (polling del frontend + worker). Null si no existe.
    /// </summary>
    Task<Auditoria?> ObtenerPorIdOrNullAsync(int auditoriaId, CancellationToken ct);

    /// <summary>
    /// Marca la auditoría como Completada y setea la fecha de finalización.
    /// </summary>
    Task MarcarCompletadaAsync(int auditoriaId, CancellationToken ct);

    /// <summary>
    /// Marca la auditoría como Fallida, setea la fecha de finalización y guarda
    /// el resumen del fallo (categoría + mensaje legible) para mostrarlo sin join.
    /// </summary>
    Task MarcarFallidaAsync(
        int auditoriaId, CategoriaErrorAuditoria categoria, string mensaje, CancellationToken ct);

    /// <summary>
    /// Devuelve el log de errores registrados para una auditoría, más reciente
    /// primero. Lista vacía si no hubo errores.
    /// </summary>
    Task<IReadOnlyList<RegistroErrorAuditoria>> ObtenerErroresAsync(
        int auditoriaId, CancellationToken ct);

    /// <summary>
    /// Trae la auditoría con artefactos evaluados, hallazgos y documentos analizados incluidos.
    /// Null si no existe.
    /// </summary>
    Task<Auditoria?> ObtenerConResultadoOrNullAsync(int id, CancellationToken ct);
}