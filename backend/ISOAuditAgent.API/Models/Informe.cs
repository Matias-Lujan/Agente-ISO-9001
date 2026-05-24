namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa un informe de auditoria generado por el sistema.
/// Mapea exactamente a la tabla "informe" en Supabase.
/// </summary>
public class Informe
{
    public int Id { get; set; }

    // Auditoria a la que pertenece este informe
    public int AuditoriaId { get; set; }

    // Fecha en que se genero el informe
    public DateTime FechaGeneracion { get; set; } = DateTime.UtcNow;

    // Tipo de informe:
    // automatico = generado por el sistema al finalizar el workflow de agentes
    // manual = generado bajo demanda por un usuario
    public string Tipo { get; set; } = TiposInforme.Automatico;

    // El contenido del informe en texto
    // Pendiente: definir el formato exacto (texto, PDF, Word, referencia externa)
    public string? Contenido { get; set; }

    // Permite deshabilitar informes sin borrarlos
    public bool Activo { get; set; } = true;
}

/// <summary>
/// Tipos posibles de informe.
/// </summary>
public static class TiposInforme
{
    // Generado automaticamente al finalizar la auditoria
    public const string Automatico = "automatico";
    // Generado bajo demanda por un usuario
    public const string Manual = "manual";
}