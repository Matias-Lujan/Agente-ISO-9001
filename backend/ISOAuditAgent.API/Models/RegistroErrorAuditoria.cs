namespace ISOAuditAgent.API.Models;

/// <summary>
/// Registro persistente de un error ocurrido durante la ejecución de una
/// auditoría. Una fila por evento de error.
///
/// Es el "log durable" de fallos: a diferencia de los logs de consola, sobrevive
/// reinicios del proceso y se puede consultar por SQL o por la API. Lo escriben
/// los nodos del workflow (con su <see cref="Nodo"/>) y, para fallos ocurridos
/// fuera de un nodo (resolución de contexto, persistencia, etc.), el runner con
/// <see cref="Nodo"/> en null.
/// </summary>
public class RegistroErrorAuditoria
{
    public int Id { get; set; }

    public int AuditoriaId { get; set; }

    /// <summary>Nodo del workflow donde ocurrió el fallo. Null si el error se
    /// produjo fuera de un nodo LLM (resolución de contexto, persistencia, etc.).</summary>
    public NodoWorkflow? Nodo { get; set; }

    public CategoriaErrorAuditoria Categoria { get; set; }

    /// <summary>Mensaje legible, pensado para mostrarle al auditor qué pasó.</summary>
    public string Mensaje { get; set; } = string.Empty;

    /// <summary>Detalle técnico completo (tipo de excepción + stack) para
    /// diagnóstico del equipo. No se expone al auditor en la UI.</summary>
    public string? DetalleTecnico { get; set; }

    public DateTime FechaUtc { get; set; }

    public Auditoria Auditoria { get; set; } = null!;
}
