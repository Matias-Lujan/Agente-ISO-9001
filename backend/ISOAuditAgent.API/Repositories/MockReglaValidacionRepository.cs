using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementación mock de IReglaValidacionRepository para pruebas y desarrollo.
/// Devuelve reglas de validación simuladas sin conectarse a la base de datos MySQL.
/// Útil para verificar la lógica del agente sin dependencias de BD.
/// </summary>
public class MockReglaValidacionRepository : IReglaValidacionRepository
{
    /// <summary>
    /// Retorna reglas de validación mockeadas para un proceso específico.
    /// Simula diferentes tipos de validaciones que el agente debe detectar.
    /// </summary>
    public Task<List<ReglaValidacion>> GetReglasByProcesoAsync(int procesoId)
    {
        var reglas = new List<ReglaValidacion>();

        if (procesoId == 1) // Proceso: Gestión de Tareas y Registros de Tiempo
        {
            reglas.Add(new ReglaValidacion
            {
                Id = 1,
                ProcesoId = procesoId,
                Descripcion = "Las horas registradas en Clockify deben corresponder a tareas planificadas en Trello",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Para cada registro de Clockify, debe existir una tarea en Trello con el mismo IdTarea",
                Activa = true,
                Notas = "Detecta registros huérfanos o orfanos en Clockify"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 2,
                ProcesoId = procesoId,
                Descripcion = "El responsable en Trello debe ser coherente con quien registra tiempo en Clockify",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "El Usuario en Clockify debe coincidir con el Responsable en Trello para la misma tarea",
                Activa = true,
                Notas = "Detecta si alguien registra horas en una tarea que no le corresponde"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 3,
                ProcesoId = procesoId,
                Descripcion = "Las fechas de registros de tiempo no deben exceder la fecha de vencimiento de la tarea",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "La FechaInicio del registro en Clockify debe ser <= FechaVencimiento de la tarea en Trello",
                Activa = true,
                Notas = "Detecta registros retroactivos o fuera del plazo"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 4,
                ProcesoId = procesoId,
                Descripcion = "Debe existir al menos un registro de tiempo para tareas marcadas como completadas",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Si Estado='Completado', debe haber al menos un registro en Clockify",
                Activa = true,
                Notas = "Detecta tareas sin evidencia de trabajo realizado"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 5,
                ProcesoId = procesoId,
                Descripcion = "Las tareas 'En Progreso' deben tener registros de tiempo recientes",
                TipoObligatorioOpcional = "Opcional",
                CriterioEvaluacion = "Si Estado='En Progreso', el último registro debe ser de los últimos 3 días",
                Activa = true,
                Notas = "Opcional: se valida solo si la tarea está activa"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 6,
                ProcesoId = procesoId,
                Descripcion = "Las horas totales registradas no deben superar un límite razonable por tarea",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Suma(HorasRegistradas) por IdTarea <= 80 horas",
                Activa = true,
                Notas = "Detecta registros anómalos o errores en la entrada de datos"
            });
        }
        else if (procesoId == 2) // Proceso: Auditoría Interna
        {
            reglas.Add(new ReglaValidacion
            {
                Id = 10,
                ProcesoId = procesoId,
                Descripcion = "Todas las auditorías planificadas deben tener asignación de recursos en Clockify",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Cada tarea de auditoría debe tener al menos un registro de tiempo",
                Activa = true,
                Notas = "Asegura que todas las auditorías sean realizadas"
            });

            reglas.Add(new ReglaValidacion
            {
                Id = 11,
                ProcesoId = procesoId,
                Descripcion = "El auditor designado debe ejecutar la auditoría",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "El Usuario en Clockify debe ser igual al Responsable en Trello",
                Activa = true,
                Notas = "Detecta delegación no autorizada"
            });
        }
        else
        {
            // Regla genérica para otros procesos
            reglas.Add(new ReglaValidacion
            {
                Id = 100,
                ProcesoId = procesoId,
                Descripcion = "Validación genérica: todas las tareas planificadas deben tener registros de tiempo",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Cada tarea en estado 'Completado' o 'En Progreso' debe tener registros",
                Activa = true,
                Notas = "Regla por defecto para nuevos procesos"
            });
        }

        return Task.FromResult(reglas);
    }

    /// <summary>
    /// Obtiene una regla específica por su ID (mock).
    /// </summary>
    public Task<ReglaValidacion?> GetReglaByIdAsync(int reglaId)
    {
        var todasLasReglas = new List<ReglaValidacion>
        {
            new ReglaValidacion
            {
                Id = 1,
                ProcesoId = 1,
                Descripcion = "Las horas registradas en Clockify deben corresponder a tareas planificadas en Trello",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "Para cada registro de Clockify, debe existir una tarea en Trello con el mismo IdTarea",
                Activa = true
            },
            new ReglaValidacion
            {
                Id = 2,
                ProcesoId = 1,
                Descripcion = "El responsable en Trello debe ser coherente con quien registra tiempo en Clockify",
                TipoObligatorioOpcional = "Obligatorio",
                CriterioEvaluacion = "El Usuario en Clockify debe coincidir con el Responsable en Trello",
                Activa = true
            }
        };

        var regla = todasLasReglas.FirstOrDefault(r => r.Id == reglaId);
        return Task.FromResult(regla);
    }

    /// <summary>
    /// Obtiene todas las reglas de un proceso, incluyendo inactivas si se solicita (mock).
    /// </summary>
    public Task<List<ReglaValidacion>> GetAllReglasByProcesoAsync(int procesoId, bool incluirInactivas = false)
    {
        var todasLasReglas = new List<ReglaValidacion>
        {
            new ReglaValidacion
            {
                Id = 1,
                ProcesoId = 1,
                Descripcion = "Validación 1",
                TipoObligatorioOpcional = "Obligatorio",
                Activa = true
            },
            new ReglaValidacion
            {
                Id = 2,
                ProcesoId = 1,
                Descripcion = "Validación 2 (Inactiva)",
                TipoObligatorioOpcional = "Obligatorio",
                Activa = false
            }
        };

        var reglas = todasLasReglas.Where(r => r.ProcesoId == procesoId).ToList();

        if (!incluirInactivas)
        {
            reglas = reglas.Where(r => r.Activa).ToList();
        }

        return Task.FromResult(reglas);
    }
}
