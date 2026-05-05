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
    private readonly Kernel? _kernel;
    private readonly ILogger<ComplianceValidationAgent> _logger;

    public ComplianceValidationAgent(
        IMcpClient mcpClient,
        IReglaValidacionRepository reglaRepository,
        ILogger<ComplianceValidationAgent> logger,
        Kernel? kernel = null)
    {
        _mcpClient = mcpClient ?? throw new ArgumentNullException(nameof(mcpClient));
        _reglaRepository = reglaRepository ?? throw new ArgumentNullException(nameof(reglaRepository));
        _kernel = kernel; // Puede ser null para tests
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Valida el proceso obteniendo datos y reglas, luego delegando el análisis al LLM.
    /// </summary>
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

        // Paso 5: Delegar al LLM para análisis (será implementado en PASO 5-7)
        // Por ahora, retornamos una lista vacía como placeholder
        var hallazgos = await AnalizarConLLMAsync(contextoValidacion);

        // Paso 6: Enriquecer hallazgos con información del proceso
        foreach (var hallazgo in hallazgos)
        {
            hallazgo.ProcesoId = procesoId;
            hallazgo.ProjectId = projectId;
        }

        return hallazgos;
    }

    /// <summary>
    /// Delega el análisis a Semantic Kernel (OpenAI/Gemini).
    /// Construye el prompt dinámico, invoca el LLM y deserializa los hallazgos.
    /// </summary>
    private async Task<List<ValidationFinding>> AnalizarConLLMAsync(ContextoValidacion contexto)
    {
        try
        {
            // Si no hay Kernel (ej: en tests), retornar lista vacía
            if (_kernel == null)
            {
                _logger.LogWarning("Kernel no disponible. Retornando lista vacía de hallazgos.");
                return new List<ValidationFinding>();
            }

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
                var respuestaLlm = await _kernel.InvokePromptAsync(prompt);
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
    /// Deserializa el JSON en una lista de ValidationFinding.
    /// Mapea los campos del JSON de respuesta a las propiedades del modelo.
    /// </summary>
    private static List<ValidationFinding> DeserializarHallazgos(string jsonHallazgos)
    {
        try
        {
            using var doc = System.Text.Json.JsonDocument.Parse(jsonHallazgos);
            var root = doc.RootElement;

            if (root.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                return new List<ValidationFinding>();
            }

            var hallazgos = new List<ValidationFinding>();

            foreach (var elemento in root.EnumerateArray())
            {
                var hallazgo = new ValidationFinding
                {
                    Id = Guid.NewGuid().ToString(),
                    Descripcion = elemento.TryGetProperty("descripcion", out var desc) 
                        ? desc.GetString() ?? "" 
                        : "",
                    Fuente = elemento.TryGetProperty("fuente", out var fuente) 
                        ? fuente.GetString() ?? "" 
                        : "Trello vs Clockify",
                    IdTareaRelacionada = elemento.TryGetProperty("idTareaRelacionada", out var idTarea) 
                        ? idTarea.GetString() 
                        : null,
                    ReglaIncumplida = elemento.TryGetProperty("reglaIncumplida", out var regla) 
                        ? regla.GetString() ?? "" 
                        : "",
                    DetallesJSON = elemento.TryGetProperty("detallesJSON", out var detalles) 
                        ? detalles.GetString() 
                        : null
                };

                hallazgos.Add(hallazgo);
            }

            return hallazgos;
        }
        catch (Exception ex)
        {
            System.Console.WriteLine($"Error deserializando hallazgos: {ex.Message}");
            return new List<ValidationFinding>();
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
