using System.Linq;
using System.Text.Json;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Repositories;
using Microsoft.SemanticKernel;

namespace ISOAuditAgent.API.Agents.ComplianceValidation;

/// <summary>
/// Implementación del Agente de Validación de Cumplimiento.
/// Coordina la obtención de datos desde Trello y Clockify (vía MCP) y las reglas de validación (vía Repository).
/// Luego delega el análisis al LLM (Semantic Kernel con Gemini).
/// </summary>
public class ComplianceValidationAgent : IComplianceValidationAgent
{
    private readonly IMcpClient _mcpClient;
    private readonly IReglaValidacionRepository _reglaRepository;
    private readonly Kernel _kernel;
    private readonly ILogger<ComplianceValidationAgent> _logger;

    public ComplianceValidationAgent(
        IMcpClient mcpClient,
        IReglaValidacionRepository reglaRepository,
        ILogger<ComplianceValidationAgent> logger,
        Kernel kernel)
    {
        _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
        _reglaRepository = reglaRepository ?? throw new ArgumentNullException(nameof(reglaRepository));
        _kernel = kernel ?? throw new ArgumentNullException(nameof(kernel));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Ejecuta el análisis de validación de cumplimiento de manera asincrónica.
    /// Punto de entrada para la arquitectura MAF (Microsoft Agent Framework).
    /// 
    /// Obtiene datos desde MCP (Trello y Clockify), consulta reglas, y delega análisis al LLM.
    /// Retorna hallazgos preliminares con evidencias y justificaciones.
    /// </summary>
    public async Task<HallazgosPreliminares> ExecuteAsync(DocumentosExtraidos input)
    {
        Console.WriteLine($"[TAILORING DEBUG] Cantidad de reglas recibidas: {input.ReglasTailoring?.Count ?? 0}");
        if (input.ReglasTailoring?.Any() == true)
        {
            foreach (var regla in input.ReglasTailoring)
            {
                Console.WriteLine($"  - {regla}");
            }
        }

        var auditoriaId = input.AuditoriaId;
        var proyectoId = input.ProyectoId;

        // Para esta implementación, usamos un procesoId por defecto
        // En una arquitectura completa, esto vendría del input o de metadatos asociados
        int procesoId = 1; // TODO: Obtener dinámicamente del contexto

        try
        {
            // Paso 1: Obtener datos desde MCP (Trello y Clockify)
            // Convertir proyectoId de int a string para pasar a métodos MCP
            var proyectoIdStr = proyectoId.ToString();
            var tareasTrello = await _mcpClient.GetTareasTrelloAsync(proyectoIdStr);
            var registrosClockify = await _mcpClient.GetRegistrosClockifyAsync(proyectoIdStr);

            // Paso 2: Obtener reglas de validación desde la BD
            var reglas = await _reglaRepository.GetReglasByProcesoAsync(procesoId);

            // Paso 3: Agrupar reglas por tipo (Strategy Pattern)
            var reglasObligatorias = reglas
                .Where(r => r.TipoObligatorioOpcional == "Obligatorio" && r.Activa)
                .ToList();

            var reglasOpcionales = reglas
                .Where(r => r.TipoObligatorioOpcional == "Opcional" && r.Activa)
                .ToList();

            // Paso 4: Preparar contexto para el LLM
            var reglasTailoringText = input.ReglasTailoring != null && input.ReglasTailoring.Any()
                ? string.Join(Environment.NewLine, input.ReglasTailoring.Select(r => $"- {r}"))
                : "No hay reglas de tailoring específicas, proceder con validación estándar.";

            var kernelArguments = new KernelArguments();
            kernelArguments["reglasTailoring"] = reglasTailoringText;
            
            // Filtrar artefactos según exigibilidad: solo enviar al LLM los que son Exigible en esta etapa
            var artefactosExigibles = input.Artefactos
                .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible)
                .ToList();
            
            kernelArguments["documentos"] = JsonSerializer.Serialize(artefactosExigibles);
            
            Console.WriteLine($"[TAILORING DEBUG] Contenido de reglasTailoring para KernelArguments:\n{reglasTailoringText}");
            Console.WriteLine($"[DOCUMENTOS DEBUG] Documentos inyectados: {artefactosExigibles.Count} exigibles de {input.Artefactos.Count} totales");
            Console.WriteLine($"[ETAPAS DEBUG] Artefactos filtrados por Exigibilidad:\n  - Exigibles: {artefactosExigibles.Count}\n  - Pendientes próxima etapa: {input.Artefactos.Count - artefactosExigibles.Count}");

            var contextoValidacion = new ContextoValidacion
            {
                ProjectId = proyectoIdStr,
                ProcesoId = procesoId,
                TareasTrello = tareasTrello,
                RegistrosClockify = registrosClockify,
                ReglasObligatorias = reglasObligatorias,
                ReglasOpcionales = reglasOpcionales,
                FechaAnalisis = DateTime.UtcNow,
                AuditoriaId = auditoriaId.ToString(),
                ReglasTailoring = input.ReglasTailoring ?? new List<string>()
            };

            _logger.LogInformation("ReglasTailoring aplicadas: {ReglasCount}", contextoValidacion.ReglasTailoring.Count);

            // Paso 5: Delegar al LLM para análisis
            var hallazgosLLM = await AnalizarConLLMAsync(contextoValidacion, kernelArguments);

            // Paso 6: Convertir hallazgos directamente del LLM al nuevo formato (HallazgoPreliminar)
            var hallazgosPreliminares = ConvertirAHallazgosPreliminares(hallazgosLLM);

            // Paso 7: Retornar en formato MAF con AgenteOrigen
            return new HallazgosPreliminares
            {
                AuditoriaId = auditoriaId,
                ProyectoId = proyectoId,
                AgenteOrigen = Models.AgenteOrigen.ComplianceValidation,
                Hallazgos = hallazgosPreliminares
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error ejecutando ExecuteAsync del ComplianceValidationAgent para auditoría {AuditoriaId} y proyecto {ProyectoId}",
                auditoriaId, proyectoId);
            throw;
        }
    }

    /// <summary>
    /// Convierte hallazgos directamente desde la respuesta del LLM (HallazgoLLMRespuesta) 
    /// al nuevo formato de HallazgoPreliminar.
    /// Mapeo 1:1 directo sin hardcoding ni transformaciones.
    /// </summary>
    private static List<HallazgoPreliminar> ConvertirAHallazgosPreliminares(
        List<HallazgoLLMRespuesta> hallazgosLLM)
    {
        var hallazgosPreliminares = new List<HallazgoPreliminar>();

        foreach (var hallazgo in hallazgosLLM)
        {
            // Mapeo 1:1 directo: las propiedades vienen del LLM como están
            var hallazgoPreliminar = new HallazgoPreliminar
            {
                ArtefactoEsperadoId = hallazgo.ArtefactoEsperadoId,  // Directo del LLM
                Descripcion = hallazgo.Descripcion,                  // Directo del LLM
                Justificacion = hallazgo.Justificacion,              // Directo del LLM
                OrigenRegla = hallazgo.OrigenRegla                   // Directo del LLM
            };

            hallazgosPreliminares.Add(hallazgoPreliminar);
        }

        return hallazgosPreliminares;
    }

    /// <summary>
    /// Método heredado para compatibilidad con código existente.
    /// Se mantiene para transiciones graduales en la arquitectura.
    /// TODO: Remover una vez se complete la migración a MAF.
    /// </summary>
    [Obsolete("Use ExecuteAsync(DocumentosExtraidos) en su lugar.", false)]
    public async Task<List<ValidationFinding>> ValidateProcessAsync(string projectId, int procesoId)
    {
        // Paso 1: Obtener datos desde MCP (Trello y Clockify)
        var tareasTrello = await _mcpClient.GetTareasTrelloAsync(projectId);
        var registrosClockify = await _mcpClient.GetRegistrosClockifyAsync(projectId);

        // Paso 2: Obtener reglas de validación desde la BD
        var reglas = await _reglaRepository.GetReglasByProcesoAsync(procesoId);

        // Paso 3: Validación básica: agrupar reglas por tipo (Strategy Pattern)
        var reglasObligatorias = reglas
            .Where(r => r.TipoObligatorioOpcional == "Obligatorio" && r.Activa)
            .ToList();

        var reglasOpcionales = reglas
            .Where(r => r.TipoObligatorioOpcional == "Opcional" && r.Activa)
            .ToList();

        // Paso 4: Preparar contexto para el LLM
        var contextoValidacion = new ContextoValidacion
        {
            ProjectId = projectId,
            ProcesoId = procesoId,
            TareasTrello = tareasTrello,
            RegistrosClockify = registrosClockify,
            ReglasObligatorias = reglasObligatorias,
            ReglasOpcionales = reglasOpcionales,
            FechaAnalisis = DateTime.UtcNow
        };

        // Paso 5: Delegar al LLM para análisis
        var hallazgosLLM = await AnalizarConLLMAsync(contextoValidacion);

        // Paso 6: Convertir a formato legacy para compatibilidad
        var hallazgosLegacy = new List<ValidationFinding>();
        foreach (var hallazgo in hallazgosLLM)
        {
            hallazgosLegacy.Add(new ValidationFinding
            {
                Id = Guid.NewGuid().ToString(),
                Descripcion = hallazgo.Descripcion,
                Fuente = hallazgo.OrigenRegla,
                ProcesoId = procesoId,
                ProjectId = projectId,
                ReglaIncumplida = hallazgo.Justificacion,
                DetallesJSON = "{}"
            });
        }

        return hallazgosLegacy;
    }

    /// <summary>
    /// Delega el análisis a Semantic Kernel (OpenAI/Gemini).
    /// Construye el prompt dinámico, invoca el LLM y deserializa los hallazgos.
    /// Retorna los hallazgos en el formato exacto que devuelve Gemini (HallazgoLLMRespuesta).
    /// </summary>
    private async Task<List<HallazgoLLMRespuesta>> AnalizarConLLMAsync(ContextoValidacion contexto, KernelArguments? kernelArguments = null)
    {
        try
        {
            // Paso 1: Construir el prompt dinámico
            var promptConstructor = new PromptConstructor(contexto);
            var prompt = promptConstructor.ConstruirPrompt();
            _logger.LogInformation("Prompt construido para Gemini. Tamaño: {PromptLength} caracteres. Tareas={TareasCount}, Registros={RegistrosCount}, ReglasObligatorias={ReglasObligatoriasCount}, ReglasOpcionales={ReglasOpcionalesCount}.",
                prompt.Length,
                contexto.TareasTrello.Count,
                contexto.RegistrosClockify.Count,
                contexto.ReglasObligatorias.Count,
                contexto.ReglasOpcionales.Count);

            if (prompt.Length > 40000)
            {
                _logger.LogWarning("El prompt para Gemini es muy grande y puede exceder el límite de tokens: {PromptLength} caracteres.", prompt.Length);
            }

            // Paso 2: Invocar el LLM a través de Semantic Kernel
            string respuestaTexto = null;
            try
            {
                var finalArgs = kernelArguments ?? new KernelArguments();
                Console.WriteLine($"[TAILORING DEBUG] KernelArguments contiene 'reglasTailoring': {finalArgs.ContainsName("reglasTailoring")}");
                if (finalArgs.ContainsName("reglasTailoring"))
                {
                    var tailoringValue = finalArgs["reglasTailoring"];
                    Console.WriteLine($"[TAILORING DEBUG] Valor de reglasTailoring en KernelArguments:\n{tailoringValue}");
                }
                Console.WriteLine($"[TAILORING DEBUG] El prompt contiene {{{{$reglasTailoring}}}}: {prompt.Contains("{{$reglasTailoring}}")}");
                
                var respuestaLlm = await _kernel.InvokePromptAsync(prompt, finalArgs);
                respuestaTexto = respuestaLlm.ToString();
            }
            catch (Microsoft.SemanticKernel.HttpOperationException httpEx)
            {
                Console.WriteLine("\n═══════════════════════════════════════════════════════════════════════════════════════");
                Console.WriteLine("❌ ERROR DE RESPUESTA GOOGLE GEMINI - CONTENIDO EXACTO:");
                Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════");
                Console.WriteLine($"HTTP Status: {httpEx.StatusCode}");
                Console.WriteLine($"Response Content:\n{httpEx.ResponseContent}");
                Console.WriteLine("═══════════════════════════════════════════════════════════════════════════════════════\n");
                throw;
            }

            // Paso 3: Extraer JSON de la respuesta
            // El LLM puede incluir explicaciones, así que extraemos el JSON
            var jsonExtraido = ExtraerJsonDeRespuesta(respuestaTexto);

            // Paso 4: Deserializar a ValidationFinding
            var hallazgos = DeserializarHallazgos(jsonExtraido);

            return hallazgos;
        }
        catch (Exception ex)
        {
            // En caso de error grave con el LLM, registrar el detalle completo y propagar la excepción.
            System.Console.Error.WriteLine($"Error en AnalizarConLLMAsync: {ex}");
            throw;
        }
    }

    /// <summary>
    /// Extrae el JSON de la respuesta del LLM.
    /// El LLM puede incluir explicaciones adicionales, así que buscamos el array JSON.
    /// </summary>
    private static string ExtraerJsonDeRespuesta(string respuesta)
    {
        // Buscar el primer [ y el último ]
        var indiceInicio = respuesta.IndexOf('[');
        var indiceFin = respuesta.LastIndexOf(']');

        if (indiceInicio == -1 || indiceFin == -1 || indiceInicio > indiceFin)
        {
            // Si no hay JSON válido, retornar array vacío
            return "[]";
        }

        return respuesta.Substring(indiceInicio, indiceFin - indiceInicio + 1);
    }

    /// <summary>
    /// Deserializa el JSON en una lista de HallazgoLLMRespuesta.
    /// Parsea exactamente la estructura que devuelve Gemini.
    /// </summary>
    private static List<HallazgoLLMRespuesta> DeserializarHallazgos(string respuestaLLM)
{
    try
    {
        // 1. Limpieza extrema
        var cleanJson = respuestaLLM.Replace("```json", "", StringComparison.OrdinalIgnoreCase)
                                    .Replace("```", "")
                                    .Trim();

        // 2. Ruteo inteligente basado en el primer caracter
        if (cleanJson.StartsWith("["))
        {
            // El LLM devolvió un array directo
            var hallazgosDirectos = JsonSerializer.Deserialize<List<HallazgoLLMRespuesta>>(cleanJson);
            return hallazgosDirectos ?? new List<HallazgoLLMRespuesta>();
        }
        else if (cleanJson.StartsWith("{"))
        {
            // El LLM devolvió el objeto esperado
            var respuesta = JsonSerializer.Deserialize<RespuestaHallazgosLLM>(cleanJson);
            return respuesta?.Hallazgos ?? new List<HallazgoLLMRespuesta>();
        }
        else
        {
            throw new Exception("El JSON limpio no empieza con '{' ni con '['.");
        }
    }
    catch (Exception ex)
    {
        System.Console.WriteLine($"Error deserializando hallazgos: {ex.Message}");
        // ESTA ES LA CLAVE: Si falla, imprimimos la evidencia del delito
        System.Console.WriteLine($"[EVIDENCIA LLM] Texto crudo recibido:\n{respuestaLLM}");
        return new List<HallazgoLLMRespuesta>();
    }
}
}

/// <summary>
/// Clase interna que agrupa el contexto necesario para el análisis LLM.
/// Implementa el Strategy Pattern: organiza datos según su tipo y criticidad.
/// </summary>
internal class ContextoValidacion
{
    /// <summary>
    /// Identificador único de la auditoría asociada a este análisis.
    /// </summary>
    public string AuditoriaId { get; set; } = string.Empty;

    /// <summary>
    /// Reglas de tailoring dinámico aplicadas a este análisis.
    /// </summary>
    public List<string> ReglasTailoring { get; set; } = new();

    /// <summary>
    /// Identificador del proyecto que se está validando.
    /// </summary>
    public string ProjectId { get; set; } = string.Empty;

    /// <summary>
    /// Identificador del proceso ISO 9001.
    /// </summary>
    public int ProcesoId { get; set; }

    /// <summary>
    /// Tareas planificadas en Trello.
    /// </summary>
    public List<TrelloTaskDto> TareasTrello { get; set; } = [];

    /// <summary>
    /// Registros de tiempo en Clockify.
    /// </summary>
    public List<ClockifyRecordDto> RegistrosClockify { get; set; } = [];

    /// <summary>
    /// Reglas de validación obligatorias (siempre se validan).
    /// Strategy 1: Validaciones críticas que no pueden faltar.
    /// </summary>
    public List<ReglaValidacion> ReglasObligatorias { get; set; } = [];

    /// <summary>
    /// Reglas de validación opcionales (se validan condicionalmente).
    /// Strategy 2: Validaciones contextuales que aplican en ciertos casos.
    /// </summary>
    public List<ReglaValidacion> ReglasOpcionales { get; set; } = [];

    /// <summary>
    /// Timestamp del análisis.
    /// </summary>
    public DateTime FechaAnalisis { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Convierte el contexto a un formato JSON que será enviado al LLM.
    /// </summary>
    public string ToJsonString()
    {
        return System.Text.Json.JsonSerializer.Serialize(this, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true
        });
    }
}
