using ISOAuditAgent.API.DTOs;

namespace ISOAuditAgent.API.Integrations.MCP;

/// <summary>
/// Implementación mock de IMcpClient para pruebas y desarrollo.
/// Devuelve datos falsos (mockeados) sin conectarse a Trello ni Clockify.
/// Útil para verificar la lógica del agente de validación sin dependencias externas.
/// </summary>
public class MockMcpClient : IMcpClient
{
    /// <summary>
    /// Retorna una lista de tareas Trello mockeadas.
    /// Simula un proyecto con varias tareas en diferentes estados.
    /// </summary>
    public Task<List<TrelloTaskDto>> GetTareasTrelloAsync(string projectId)
    {
        var tareasMock = new List<TrelloTaskDto>
        {
            new TrelloTaskDto
            {
                Id = "TRE-001",
                Nombre = "Documentación de Procesos",
                Responsable = "Juan Pérez",
                Estado = "En Progreso",
                FechaInicio = new DateTime(2024, 01, 08),
                FechaVencimiento = new DateTime(2024, 01, 15),
                FechaFinalizacion = null,
                ProjectId = projectId,
                Etiquetas = ["ISO9001", "Documentación"]
            },
            new TrelloTaskDto
            {
                Id = "TRE-002",
                Nombre = "Validación de Registros",
                Responsable = "María García",
                Estado = "Completado",
                FechaInicio = new DateTime(2024, 01, 10),
                FechaVencimiento = new DateTime(2024, 01, 18),
                FechaFinalizacion = new DateTime(2024, 01, 18),
                ProjectId = projectId,
                Etiquetas = ["ISO9001", "Validación"]
            },
            new TrelloTaskDto
            {
                Id = "TRE-003",
                Nombre = "Auditoría Interna",
                Responsable = "Carlos López",
                Estado = "Por Iniciar",
                FechaInicio = new DateTime(2024, 01, 22),
                FechaVencimiento = new DateTime(2024, 02, 05),
                FechaFinalizacion = null,
                ProjectId = projectId,
                Etiquetas = ["ISO9001", "Auditoría"]
            },
            new TrelloTaskDto
            {
                Id = "TRE-004",
                Nombre = "Capacitación del Equipo",
                Responsable = "Juan Pérez",
                Estado = "En Progreso",
                FechaInicio = new DateTime(2024, 01, 15),
                FechaVencimiento = new DateTime(2024, 01, 25),
                FechaFinalizacion = null,
                ProjectId = projectId,
                Etiquetas = ["ISO9001", "Capacitación"]
            }
        };

        return Task.FromResult(tareasMock);
    }

    /// <summary>
    /// Retorna una lista de registros de Clockify mockeados.
    /// Simula registros de tiempo para varias tareas y usuarios.
    /// </summary>
    public Task<List<ClockifyRecordDto>> GetRegistrosClockifyAsync(string projectId)
    {
        var registrosMock = new List<ClockifyRecordDto>
        {
            new ClockifyRecordDto
            {
                Id = "CLO-001",
                IdTarea = "TRE-001",
                Usuario = "Juan Pérez",
                HorasRegistradas = 4.5m,
                FechaInicio = new DateTime(2024, 01, 08, 09, 00, 00),
                FechaFin = new DateTime(2024, 01, 08, 13, 30, 00),
                Descripcion = "Análisis inicial de procesos",
                ProjectId = projectId,
                Etiquetas = ["Documentación"]
            },
            new ClockifyRecordDto
            {
                Id = "CLO-002",
                IdTarea = "TRE-001",
                Usuario = "Juan Pérez",
                HorasRegistradas = 3m,
                FechaInicio = new DateTime(2024, 01, 09, 10, 00, 00),
                FechaFin = new DateTime(2024, 01, 09, 13, 00, 00),
                Descripcion = "Redacción de documentos",
                ProjectId = projectId,
                Etiquetas = ["Documentación"]
            },
            new ClockifyRecordDto
            {
                Id = "CLO-003",
                IdTarea = "TRE-002",
                Usuario = "María García",
                HorasRegistradas = 5.5m,
                FechaInicio = new DateTime(2024, 01, 10, 08, 00, 00),
                FechaFin = new DateTime(2024, 01, 10, 13, 30, 00),
                Descripcion = "Validación de datos en sistema",
                ProjectId = projectId,
                Etiquetas = ["Validación"]
            },
            new ClockifyRecordDto
            {
                Id = "CLO-004",
                IdTarea = "TRE-002",
                Usuario = "María García",
                HorasRegistradas = 4m,
                FechaInicio = new DateTime(2024, 01, 18, 09, 00, 00),
                FechaFin = new DateTime(2024, 01, 18, 13, 00, 00),
                Descripcion = "Cierre de validación",
                ProjectId = projectId,
                Etiquetas = ["Validación"]
            },
            new ClockifyRecordDto
            {
                Id = "CLO-005",
                IdTarea = "TRE-004",
                Usuario = "Juan Pérez",
                HorasRegistradas = 2m,
                FechaInicio = new DateTime(2024, 01, 15, 14, 00, 00),
                FechaFin = new DateTime(2024, 01, 15, 16, 00, 00),
                Descripcion = "Preparación de materiales de capacitación",
                ProjectId = projectId,
                Etiquetas = ["Capacitación"]
            },
            // Registro de una tarea que no existe en Trello (inconsistencia simulada)
            new ClockifyRecordDto
            {
                Id = "CLO-006",
                IdTarea = "TRE-999",
                Usuario = "Ricardo Sánchez",
                HorasRegistradas = 6m,
                FechaInicio = new DateTime(2024, 01, 16, 08, 00, 00),
                FechaFin = new DateTime(2024, 01, 16, 14, 00, 00),
                Descripcion = "Trabajo no asignado en Trello",
                ProjectId = projectId,
                Etiquetas = ["Orfano"]
            },
            // Registro con discrepancia de responsable (inconsistencia simulada)
            new ClockifyRecordDto
            {
                Id = "CLO-007",
                IdTarea = "TRE-001",
                Usuario = "Carlos López",
                HorasRegistradas = 2.5m,
                FechaInicio = new DateTime(2024, 01, 20, 09, 00, 00),
                FechaFin = new DateTime(2024, 01, 20, 11, 30, 00),
                Descripcion = "Horas registradas por usuario diferente",
                ProjectId = projectId,
                Etiquetas = ["Documentación"]
            }
        };

        return Task.FromResult(registrosMock);
    }
}
