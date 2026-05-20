using ISOAuditAgent.API.DTOs;

namespace ISOAuditAgent.API.Integrations.MCP;

/// <summary>
/// Define el contrato para la comunicación con el servidor MCP (Model Context Protocol).
/// El agente de validación comunica ÚNICAMENTE a través de esta interfaz.
/// NO accede directamente a Trello ni Clockify.
/// </summary>
public interface IMcpClient
{
    /// <summary>
    /// Obtiene las tareas de Trello para un proyecto específico.
    /// Esta llamada se realiza a través del servidor MCP que expone la tool "get_tareas_trello".
    /// </summary>
    /// <param name="projectId">Identificador del proyecto en Trello.</param>
    /// <returns>Lista de tareas de Trello como DTOs.</returns>
    Task<List<TrelloTaskDto>> GetTareasTrelloAsync(string projectId);

    /// <summary>
    /// Obtiene los registros de tiempo de Clockify para un proyecto específico.
    /// Esta llamada se realiza a través del servidor MCP que expone la tool "get_registros_clockify".
    /// </summary>
    /// <param name="projectId">Identificador del proyecto en Clockify.</param>
    /// <returns>Lista de registros de tiempo como DTOs.</returns>
    Task<List<ClockifyRecordDto>> GetRegistrosClockifyAsync(string projectId);
}
