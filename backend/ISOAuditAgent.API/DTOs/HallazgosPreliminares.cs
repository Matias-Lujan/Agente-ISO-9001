using System.Text.Json.Serialization;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.DTOs;

/// <summary>
/// Contiene los hallazgos preliminares generados por un agente de análisis.
/// Representa la salida (output) de los agentes en la arquitectura MAF.
/// Se utiliza para pasar datos entre agentes en el grafo de procesamiento.
/// </summary>
public sealed record HallazgosPreliminares
{
    /// <summary>
    /// Identificador único de la auditoría para la cual se generaron estos hallazgos.
    /// </summary>
    [JsonPropertyName("auditoriaId")]
    public int AuditoriaId { get; init; }

    /// <summary>
    /// Identificador del proyecto relacionado con estos hallazgos.
    /// </summary>
    [JsonPropertyName("proyectoId")]
    public int ProyectoId { get; init; }

    /// <summary>
    /// Enumeración que indica cuál agente generó estos hallazgos.
    /// Facilita el rastreo de origen en la arquitectura MAF.
    /// </summary>
    [JsonPropertyName("agenteOrigen")]
    public AgenteOrigen AgenteOrigen { get; init; }

    /// <summary>
    /// Colección de hallazgos preliminares detectados por el agente.
    /// Cada hallazgo contiene descripción, evidencias y justificación.
    /// </summary>
    [JsonPropertyName("hallazgos")]
    public IReadOnlyList<HallazgoPreliminar> Hallazgos { get; init; } = new List<HallazgoPreliminar>();
}
