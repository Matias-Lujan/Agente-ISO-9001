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
}