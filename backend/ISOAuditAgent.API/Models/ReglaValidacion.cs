namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa una regla de validacion configurable por proceso.
/// Cumple con RF-04 del ERS: las reglas no son fijas, son configurables
/// por el Administrador desde la interfaz.
/// Mapea a la tabla "regla_validacion" en Supabase.
/// </summary>
public class ReglaValidacion
{
    public int Id { get; set; }

    // Procedimiento al que pertenece la regla
    public int ProcedimientoId { get; set; }

    // Etapa especifica a la que aplica la regla
    // Es nullable porque algunas reglas aplican a todo el procedimiento
    public int? EtapaId { get; set; }

    // Codigo identificador de la regla. Ej: "RGL-001"
    public string? Codigo { get; set; }

    // Descripcion clara de que se valida
    public string Descripcion { get; set; } = string.Empty;

    // Tipo de validacion que realiza la regla
    public string Tipo { get; set; } = TiposRegla.Existencia;

    // Si es true, su incumplimiento genera una No Conformidad
    // Si es false, puede generar solo una Observacion
    public bool Obligatoria { get; set; } = true;

    // Permite deshabilitar una regla sin borrarla
    public bool Activa { get; set; } = true;
}

/// <summary>
/// Tipos de regla de validacion.
/// Define que aspecto del proceso valida cada regla.
/// </summary>
public static class TiposRegla
{
    // Verifica que un documento o registro exista
    public const string Existencia   = "existencia";
    // Verifica que el contenido de un documento sea correcto
    public const string Contenido    = "contenido";
    // Verifica consistencia entre documentos relacionados
    public const string Consistencia = "consistencia";
    // Verifica que las actividades se hayan ejecutado en el orden correcto
    public const string Secuencia    = "secuencia";
    // Verifica que haya responsables asignados
    public const string Responsable  = "responsable";
}