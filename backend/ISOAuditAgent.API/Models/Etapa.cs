namespace ISOAuditAgent.API.Models;

public class Etapa
{
    public int Id { get; set; }
    public int ProcedimientoId { get; set; }
    public string Nombre { get; set; } = null!;
    public int Orden { get; set; }
    public string? Descripcion { get; set; }

    // Navegación hacia el "uno": la etapa pertenece a un procedimiento.
    public Procedimiento Procedimiento { get; set; } = null!;

    // Navegación hacia los "muchos".
    public ICollection<ArtefactoEsperado> ArtefactosEsperados { get; set; } = new List<ArtefactoEsperado>();
    public ICollection<Auditoria> Auditorias { get; set; } = new List<Auditoria>();
}