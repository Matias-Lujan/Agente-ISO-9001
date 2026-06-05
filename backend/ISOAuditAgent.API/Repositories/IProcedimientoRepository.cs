using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Interfaz del repositorio de procedimientos, etapas y artefactos.
/// Define que operaciones existen sin decir como se implementan.
/// </summary>
public interface IProcedimientoRepository
{
    // ── PROCEDIMIENTOS ────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve todos los procedimientos.
    /// </summary>
    Task<IReadOnlyList<Procedimiento>> ObtenerTodosAsync();

    /// <summary>
    /// Busca un procedimiento por su ID.
    /// </summary>
    Task<Procedimiento?> ObtenerPorIdAsync(int id);

    // ── ETAPAS ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Devuelve las etapas de un procedimiento en orden.
    /// </summary>
    Task<IReadOnlyList<Etapa>> ObtenerEtapasPorProcedimientoAsync(int procedimientoId);

    /// <summary>
    /// Busca una etapa por su ID.
    /// </summary>
    Task<Etapa?> ObtenerEtapaPorIdAsync(int id);

    // ── ARTEFACTOS ESPERADOS ──────────────────────────────────────────────────

    /// <summary>
    /// Devuelve los artefactos esperados de una etapa.
    /// </summary>
    Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosPorEtapaAsync(int etapaId);

    /// <summary>
    /// Busca un artefacto esperado por su ID.
    /// </summary>
    Task<ArtefactoEsperado?> ObtenerArtefactoPorIdAsync(int id);

    /// <summary>
    /// Crea un artefacto esperado nuevo.
    /// </summary>
    Task<ArtefactoEsperado> CrearArtefactoAsync(ArtefactoEsperado artefacto);

    /// <summary>
    /// Actualiza un artefacto esperado existente.
    /// </summary>
    Task<ArtefactoEsperado> ActualizarArtefactoAsync(ArtefactoEsperado artefacto);

    // ── Usados por el workflow del orchestrator (portado de dev) ──────────────

    /// <summary>
    /// Trae todas las etapas de un procedimiento, ordenadas por 'orden'.
    /// </summary>
    Task<IReadOnlyList<Etapa>> ObtenerEtapasAsync(int procedimientoId, CancellationToken ct);

    /// <summary>
    /// Trae TODOS los ArtefactoEsperado del procedimiento con su Etapa cargada
    /// (Include obligatorio: ResolutorContextoService usa Etapa.Orden).
    /// </summary>
    Task<IReadOnlyList<ArtefactoEsperado>> ObtenerArtefactosEsperadosAsync(
        int procedimientoId, CancellationToken ct);
}