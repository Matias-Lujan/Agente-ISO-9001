namespace ISOAuditAgent.API.Models;

/// <summary>
/// Una versión del system prompt de un agente LLM. La tabla es append-only:
/// cada guardado inserta una fila nueva con Version incremental y EsActiva=true,
/// desactivando la anterior. Así queda el historial completo (quién cambió qué y
/// cuándo) con rollback a cualquier versión — apropiado para trazabilidad ISO.
///
/// El prompt en uso para un agente es la fila con EsActiva=true de ese AgenteKey.
/// </summary>
public class PromptAgente
{
    public int Id { get; set; }

    /// <summary>Key del agente: "DocumentAnalysis", "ComplianceValidation", etc.</summary>
    public string AgenteKey { get; set; } = null!;

    /// <summary>Versión incremental (1..N) por AgenteKey.</summary>
    public int Version { get; set; }

    /// <summary>Texto del system prompt (mapea a longtext en MySQL).</summary>
    public string Contenido { get; set; } = null!;

    /// <summary>La versión activa (en uso). Una sola por AgenteKey.</summary>
    public bool EsActiva { get; set; }

    /// <summary>Usuario que creó la versión. Null = seed del sistema.</summary>
    public int? ModificadoPorUsuarioId { get; set; }
    public Usuario? ModificadoPor { get; set; }

    public DateTime FechaCreacion { get; set; }

    /// <summary>Nota opcional del cambio (ej. "Restablecido al valor por defecto").</summary>
    public string? Comentario { get; set; }
}
