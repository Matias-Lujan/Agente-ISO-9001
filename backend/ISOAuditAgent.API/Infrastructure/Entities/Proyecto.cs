using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.Infrastructure.Entities;

/// <summary>
/// Proyecto de BDT Global asociado a un procedimiento que lo rige y a
/// las integraciones externas opcionales (Drive, Trello, Clockify).
/// </summary>
/// <remarks>
/// Modelo alineado con <c>modelo_bd.md</c> §3 (tabla <c>Proyecto</c>).
/// <see cref="TipoProyecto"/> se deriva de <see cref="HorasEstimadas"/>
/// (&gt; 1200 = A, ≤ 1200 = B) y se persiste para no recalcularlo en cada
/// consulta. Las integraciones (Drive, Trello, Clockify) son nullables
/// porque un proyecto puede no usar todas las herramientas.
/// </remarks>
public sealed class Proyecto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateOnly? FechaInicio { get; set; }
    public DateOnly? FechaFin { get; set; }
    public TipoProyecto TipoProyecto { get; set; }
    public decimal HorasEstimadas { get; set; }
    public int ProcedimientoId { get; set; }

    public string? TrelloBoardId { get; set; }
    public string? ClockifyProjectId { get; set; }
    public string? DriveFolderId { get; set; }

    public bool Activo { get; set; } = true;

    public Procedimiento? Procedimiento { get; set; }
}
