using System.Net;
using ISOAuditAgent.API.Agents.Shared;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Orchestrator;

/// <summary>
/// Traduce una excepción a una <see cref="CategoriaErrorAuditoria"/> y un mensaje
/// legible para el auditor. Es la pieza que convierte "algo falló" en "falló
/// porque el servicio de IA devolvió un JSON inválido".
///
/// Se apoya primero en las excepciones tipadas del dominio
/// (<see cref="ContextoAuditoriaException"/>, <see cref="RespuestaLlmInvalidaException"/>,
/// <see cref="EnsambleAuditoriaException"/>) y, para el resto, aplica heurísticas
/// sobre el tipo y el mensaje. Es determinista y sin efectos: se puede llamar
/// tanto desde un nodo (excepción directa) como desde el runner (excepción ya
/// envuelta por el WorkflowErrorEvent).
/// </summary>
public static class ClasificadorErrorAuditoria
{
    public static (CategoriaErrorAuditoria Categoria, string Mensaje) Clasificar(Exception ex)
    {
        var real = Desenvolver(ex);

        return real switch
        {
            ContextoAuditoriaException => (
                CategoriaErrorAuditoria.ConfiguracionProyecto,
                "Faltan datos o configuración del proyecto para poder auditar. " +
                real.Message),

            // El mensaje ya viene redactado para el auditor (incluye qué documento).
            DocumentoDriveNoEncontradoException => (
                CategoriaErrorAuditoria.DocumentoNoEncontrado,
                real.Message),

            RespuestaLlmInvalidaException => (
                CategoriaErrorAuditoria.RespuestaIAInvalida,
                "El servicio de IA devolvió una respuesta con formato inválido y no " +
                "se pudo interpretar. Reintentá la auditoría; si persiste, revisá el " +
                "prompt del agente."),

            EnsambleAuditoriaException => (
                CategoriaErrorAuditoria.ErrorInterno,
                "Error interno al consolidar el resultado de la auditoría."),

            HttpRequestException or TaskCanceledException or TimeoutException or WebException => (
                CategoriaErrorAuditoria.ServicioIA,
                "El servicio de IA no respondió a tiempo tras varios intentos. " +
                "Puede ser una demora o corte temporal; reintentá en unos minutos."),

            _ => ClasificarPorHeuristica(real)
        };
    }

    private static (CategoriaErrorAuditoria, string) ClasificarPorHeuristica(Exception ex)
    {
        var msg = ex.Message ?? string.Empty;

        // Configuración del proyecto: carpeta de Drive no cargada.
        if (Contiene(msg, "DriveFolderId") || Contiene(msg, "no tiene una carpeta"))
        {
            return (CategoriaErrorAuditoria.ConfiguracionProyecto,
                "El proyecto no tiene configurada la carpeta de Drive necesaria para " +
                "descargar la evidencia.");
        }

        // Integración externa: fallo hablando con Drive / Trello / Clockify / MCP.
        if (Contiene(msg, "Drive") || Contiene(msg, "Trello") || Contiene(msg, "Clockify")
            || Contiene(msg, "MCP"))
        {
            return (CategoriaErrorAuditoria.IntegracionExterna,
                "No se pudo obtener la evidencia desde una integración externa " +
                "(Drive, Trello o Clockify). Verificá la conexión y las credenciales.");
        }

        // Respuesta del LLM sin formato válido (por si algún parser todavía lanza
        // InvalidOperationException en vez de RespuestaLlmInvalidaException).
        if (Contiene(msg, "JSON") || Contiene(msg, "LLM") || Contiene(msg, "deserializ"))
        {
            return (CategoriaErrorAuditoria.RespuestaIAInvalida,
                "El servicio de IA devolvió una respuesta con formato inválido y no " +
                "se pudo interpretar.");
        }

        return (CategoriaErrorAuditoria.ErrorInterno,
            "Ocurrió un error interno durante la ejecución de la auditoría. " +
            "Revisá el detalle técnico del registro de errores.");
    }

    /// <summary>
    /// El runner envuelve la excepción real del workflow dentro de un
    /// <see cref="InvalidOperationException"/> ("emitió un WorkflowErrorEvent").
    /// Acá bajamos a la causa real para clasificarla correctamente.
    /// </summary>
    private static Exception Desenvolver(Exception ex)
    {
        var actual = ex;
        while (actual is InvalidOperationException
               && actual.InnerException is not null
               && Contiene(actual.Message, "WorkflowErrorEvent"))
        {
            actual = actual.InnerException;
        }
        return actual;
    }

    private static bool Contiene(string texto, string fragmento) =>
        texto.Contains(fragmento, StringComparison.OrdinalIgnoreCase);
}
