namespace ISOAuditAgent.API.Models;

/// <summary>
/// Consumo de tokens de una única llamada al LLM (Gemini). Se inserta una fila
/// por llamada al terminar la auditoría que la generó. Permite armar el KPI de
/// consumo de la app: total y desglose por agente.
///
/// AuditoriaId es nullable e informativa (sin FK): si en el futuro se borrara la
/// auditoría, el registro de consumo histórico se conserva igual.
/// </summary>
public class ConsumoTokens
{
    public int Id { get; set; }

    /// <summary>Auditoría que originó la llamada. Null si no se pudo atribuir.</summary>
    public int? AuditoriaId { get; set; }

    /// <summary>Agente que hizo la llamada: "DocumentAnalysis", "ComplianceValidation", etc.</summary>
    public string AgenteKey { get; set; } = null!;

    /// <summary>Modelo de IA usado (ej. "gemini-2.5-flash").</summary>
    public string Modelo { get; set; } = null!;

    public int TokensEntrada { get; set; }
    public int TokensSalida { get; set; }
    public int TokensTotal { get; set; }

    public DateTime FechaHoraUtc { get; set; }
}
