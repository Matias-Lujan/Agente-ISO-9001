namespace ISOAuditAgent.API.Models;

/// <summary>
/// Representa un procedimiento de la empresa.
/// En BDT Global el procedimiento principal es el PR 11-13.
/// Mapea a la tabla "procedimiento" en Supabase.
/// </summary>
public class Procedimiento
{
    public int Id { get; set; }

    // Codigo formal del procedimiento. Ej: "PR-11-13"
    public string Codigo { get; set; } = string.Empty;

    // Nombre completo. Ej: "Diseño y Analisis Desarrollo e Implementacion de Software"
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }
}

/// <summary>
/// Representa una etapa dentro de un procedimiento.
/// Cada procedimiento tiene varias etapas en orden.
/// Ej: Planificacion, Analisis, Desarrollo, Testing, Implementacion.
/// Mapea a la tabla "etapa" en Supabase.
/// </summary>
public class Etapa
{
    public int Id { get; set; }

    // Procedimiento al que pertenece esta etapa
    public int ProcedimientoId { get; set; }

    // Nombre de la etapa. Ej: "Planificacion", "Testing"
    public string Nombre { get; set; } = string.Empty;

    // Numero que define el orden cronologico de la etapa
    // Util para mostrar las etapas en secuencia en el frontend
    public int Orden { get; set; }

    public string? Descripcion { get; set; }
}

/// <summary>
/// Representa un artefacto (documento) que se espera en una etapa.
/// Define que documentos deben existir segun el procedimiento.
/// Mapea a la tabla "artefacto_esperado" en Supabase.
/// </summary>
public class ArtefactoEsperado
{
    public int Id { get; set; }

    // Etapa en la que se espera este artefacto
    public int EtapaId { get; set; }

    // Codigo formal del artefacto. Ej: "FR-30", "FR-25"
    // Nullable porque algunos artefactos no tienen formulario
    // (ej: cronograma .mmp, tarjetas de Trello)
    public string? Codigo { get; set; }

    // Nombre del artefacto. Ej: "ERS", "Cronograma"
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    // Indica si es obligatorio para proyectos tipo A (mas de 1200 horas)
    // Si es false, es "evaluar y justificar"
    public bool MandatorioTipoA { get; set; } = true;

    // Indica si es obligatorio para proyectos tipo B (hasta 1200 horas)
    // Si es false, es "evaluar y justificar"
    public bool MandatorioTipoB { get; set; } = true;

    // Ruta relativa al template del artefacto.
    // Pendiente: la ubicacion exacta depende de la respuesta del cliente.
    public string? PathTemplateRelativo { get; set; }
}