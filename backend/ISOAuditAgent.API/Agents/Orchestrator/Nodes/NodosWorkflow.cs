using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using ISOAuditAgent.API.Agents.FindingsClassification;
using ISOAuditAgent.API.Agents.ConsistencyVerification;
using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Trello;
using ISOAuditAgent.API.Agents.ComplianceValidation;
using ISOAuditAgent.API.Agents.Shared;

namespace ISOAuditAgent.API.Agents.Orchestrator;

// ============================================================================
//  [1] ResolutorContextoNode — nodo determinista inicial, sin LLM
// ============================================================================

public sealed class ResolutorContextoNode
    : Executor<IniciarAuditoriaWorkflowInput, ContextoAuditoria>
{
    private readonly ResolutorContextoService _servicio;

    public ResolutorContextoNode(ResolutorContextoService servicio)
        : base("ResolutorContexto")
    {
        _servicio = servicio;
    }

    public override async ValueTask<ContextoAuditoria> HandleAsync(
        IniciarAuditoriaWorkflowInput message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        return await _servicio.ResolverAsync(message, ct);
    }
}

// ============================================================================
//  [2] DocumentAnalysisNode — agente con LLM + I/O async pre y post LLM
// ----------------------------------------------------------------------------
public sealed class DocumentAnalysisNode
    : AgenteExecutorBase<ContextoAuditoria, DocumentosExtraidos>
{
    private const int MaxIntentos = 3;

    private readonly ITailoringSource _tailoringSource;
    private readonly IArtefactoFisicoChecker _artefactoChecker;
    private readonly TrelloChecker _trelloChecker;
    private readonly ClockifyChecker _clockifyChecker;
    private readonly IAuditoriaProgresoTracker _tracker;
    private readonly IRegistroErrorAuditoriaWriter _errorWriter;
    private readonly ILogger<DocumentAnalysisNode> _logger;

    public DocumentAnalysisNode(
        AIAgent agente,
        ITailoringSource tailoringSource,
        IArtefactoFisicoChecker artefactoChecker,
        TrelloChecker trelloChecker,
        ClockifyChecker clockifyChecker,
        IAuditoriaProgresoTracker tracker,
        IRegistroErrorAuditoriaWriter errorWriter,
        ILogger<DocumentAnalysisNode> logger)
        : base("DocumentAnalysis", agente)
    {
        _tailoringSource = tailoringSource;
        _artefactoChecker = artefactoChecker;
        _trelloChecker = trelloChecker;
        _clockifyChecker = clockifyChecker;
        _tracker = tracker;
        _errorWriter = errorWriter;
        _logger = logger;
    }

    public override async ValueTask<DocumentosExtraidos> HandleAsync(
        ContextoAuditoria message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        await _tracker.MarcarEnCursoAsync(
            message.AuditoriaId, NodoWorkflow.DocumentAnalysis, ct);

        try
        {
            var resultado = await EjecutarAsync(message, ct);

            await _tracker.MarcarCompletadoAsync(
                message.AuditoriaId, NodoWorkflow.DocumentAnalysis, ct);

            return resultado;
        }
        catch (Exception ex)
        {
            await _errorWriter.RegistrarAsync(
                message.AuditoriaId, NodoWorkflow.DocumentAnalysis, ex, CancellationToken.None);
            await _tracker.MarcarFallidoAsync(
                message.AuditoriaId, NodoWorkflow.DocumentAnalysis, CancellationToken.None);
            throw;
        }
    }

    private async Task<DocumentosExtraidos> EjecutarAsync(
        ContextoAuditoria message, CancellationToken ct)
    {
        // 1. PRE-LLM async: descargar el FR-29 del Drive del proyecto.
        var driveFolderId = message.Integraciones.DriveFolderId;
        if (string.IsNullOrWhiteSpace(driveFolderId))
        {
            throw new InvalidOperationException(
                $"Proyecto {message.ProyectoId}: no tiene DriveFolderId en " +
                "Integraciones. DocumentAnalysis necesita una carpeta de Drive " +
                "para descargar el FR-29.");
        }

        var tailoring = await _tailoringSource
            .ObtenerAsync(driveFolderId, ct)
            .ConfigureAwait(false);

        // 2. Armar el prompt (sync, sin I/O).
        var prompt = ConstruirPromptInterno(message, tailoring.Filas);

        // 3. Llamar al LLM con reintentos. La base no se usa porque HandleAsync
        //    overridea el template completo.
        var textoLlm = await LlmConReintentosAsync(prompt, message.AuditoriaId, ct);

        // 3.5. Log de la respuesta cruda — D7.3, mientras estabilizamos el
        //      formato del LLM. Si el parseo falla, el log tiene el texto
        //      exacto que devolvió Gemini. Se quita en D7.6 si ya no aporta.
        _logger.LogInformation(
            "DocumentAnalysis LLM respuesta cruda ({Len} chars):\n{Texto}",
            textoLlm.Length, textoLlm);

        // 4. Parsear el JSON del LLM (tolerante a prosa / cercas / BOM).
        var llmOutput = ParsearJsonLlm(textoLlm);

        _logger.LogInformation(
            "DocumentAnalysis: LLM respondió ({Len} chars). Iniciando ArtefactosBuilder para {N} artefactos.",
            textoLlm.Length, llmOutput.Artefactos?.Count ?? 0);
        // 5. POST-LLM async: verificar artefactos físicamente y armar el contrato 3.
        var artefactos = await ArtefactosBuilder
            .ConstruirAsync(message, llmOutput, _artefactoChecker, _trelloChecker, _clockifyChecker, ct)
            .ConfigureAwait(false);

        _logger.LogInformation(
            "DocumentAnalysis: ArtefactosBuilder completado — {N} artefactos.",
            artefactos.Count);
        var resultado = new DocumentosExtraidos(
            message.AuditoriaId,
            message.ProyectoId,
            message.EtapaId,
            message.ProcedimientoCodigo,
            message.ProcedimientoNombre,
            artefactos,
            tailoring.ResponsableProyecto,
            message.NombreProyecto);

        // 6. Defensa en profundidad: validar invariantes del contrato 3 antes
        //    de emitir el DTO al grafo.
        InvariantsValidator.Validar(resultado);

        return resultado;
    }

    private async Task<string> LlmConReintentosAsync(
        string prompt, int idAuditoria, CancellationToken ct)
    {
        Exception? ultimoError = null;
        for (int intento = 1; intento <= MaxIntentos; intento++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var respuesta = await Agente.RunAsync(prompt, cancellationToken: ct);
                return respuesta.Text ?? string.Empty;
            }
            catch (Exception ex) when (!ct.IsCancellationRequested && intento < MaxIntentos)
            {
                ultimoError = ex;
                _logger.LogWarning(ex,
                    "DocumentAnalysis: intento {Intento}/{Max} falló (error de red, auditoría {Id}). Reintentando.",
                    intento, MaxIntentos, idAuditoria);
            }
        }
        throw ultimoError!;
    }

    /// <summary>
    /// Serializa el contexto + tailoring al user message del LLM. El system
    /// prompt vive en SystemPrompts.AnalizadorDocumental (lo configura
    /// Chat D §5.4 al construir el AIAgent).
    /// </summary>
    private static string ConstruirPromptInterno(
    ContextoAuditoria contexto,
    IReadOnlyList<FilaTailoring> tailoring)
    {
        var payload = new
        {
            procedimiento = new
            {
                codigo = contexto.ProcedimientoCodigo,
                nombre = contexto.ProcedimientoNombre
            },
            contexto = new
            {
                artefactosEsperados = contexto.ArtefactosEsperados.Select(a => new
                {
                    artefactoEsperadoId = a.ArtefactoEsperadoId,
                    codigo = a.CodigoArtefacto,          // puede ser null
                    nombre = a.NombreArtefacto,          // siempre presente, fallback de match
                    exigibilidad = a.Exigibilidad.ToString(),
                    obligatoriedad = a.Obligatoriedad.ToString()
                })
            },
            tailoring = tailoring.Select(f => new
            {
                codigoArtefacto = f.CodigoArtefacto,     // puede ser null
                nombreArtefacto = f.NombreArtefacto,     // siempre presente, fallback de match
                aplica = f.Aplica,
                justificacionNoAplica = f.JustificacionNoAplica,
                urlReferencia = f.UrlReferencia
            })
        };

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    /// <summary>
    /// Deserializa el JSON del LLM, tolerando dos casos comunes:
    ///   1. Cercas markdown: ```json ... ``` (ya estaba).
    ///   2. Prosa antes/después del JSON: "Acá tenés el JSON solicitado: { ... }"
    ///      → extraemos desde el primer '{' hasta el último '}' que cierra ese
    ///      objeto raíz.
    ///   3. BOM / chars no-ASCII al inicio antes del '{'.
    ///
    /// Si después de extraer sigue sin parsear, lanza con el texto crudo
    /// incluido en el mensaje (para diagnóstico).
    /// </summary>
    private static LlmOutput ParsearJsonLlm(string textoLlm)
    {
        var jsonLimpio = ExtraerJsonObjeto(textoLlm);

        try
        {
            var resultado = System.Text.Json.JsonSerializer.Deserialize<LlmOutput>(
                jsonLimpio,
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (resultado is null)
            {
                throw new RespuestaLlmInvalidaException(
                    "Respuesta del LLM deserializó a null.");
            }

            if (resultado.Artefactos is null)
            {
                throw new RespuestaLlmInvalidaException(
                    "Respuesta del LLM no incluye la propiedad 'artefactos'.");
            }

            return resultado;
        }
        catch (System.Text.Json.JsonException ex)
        {
            // Incluimos el JSON ya extraído + un prefijo del texto crudo para
            // diagnosticar rápido qué devolvió el LLM.
            var preview = textoLlm.Length > 500
                ? textoLlm.Substring(0, 500) + "..."
                : textoLlm;

            throw new RespuestaLlmInvalidaException(
                $"Respuesta del LLM no es JSON válido: {ex.Message}. " +
                $"Texto crudo (primeros 500 chars): >>>{preview}<<<", ex);
        }
    }

    /// <summary>
    /// Quita cercas markdown, BOM, y extrae el primer objeto JSON balanceado.
    /// Si no encuentra '{...}', devuelve el texto original (que va a fallar
    /// el JsonSerializer con mensaje claro).
    /// </summary>
    private static string ExtraerJsonObjeto(string textoLlm)
    {
        if (string.IsNullOrWhiteSpace(textoLlm))
            return textoLlm;

        // 1. Quitar cercas markdown si las hay.
        var s = textoLlm
            .Replace("```json", string.Empty)
            .Replace("```", string.Empty)
            .Trim();

        // 2. Quitar BOM UTF-8 si quedó.
        if (s.Length > 0 && s[0] == '\uFEFF')
            s = s.Substring(1);

        // 3. Buscar el primer '{' y el último '}' que balancean.
        var inicio = s.IndexOf('{');
        if (inicio < 0) return s; // No hay objeto; deja que JsonSerializer falle ruidoso.

        // Recorremos contando llaves para encontrar el cierre del objeto raíz.
        int depth = 0;
        int fin = -1;
        bool dentroString = false;
        bool escape = false;

        for (int i = inicio; i < s.Length; i++)
        {
            var c = s[i];

            if (escape) { escape = false; continue; }
            if (c == '\\' && dentroString) { escape = true; continue; }
            if (c == '"') { dentroString = !dentroString; continue; }
            if (dentroString) continue;

            if (c == '{') depth++;
            else if (c == '}')
            {
                depth--;
                if (depth == 0) { fin = i; break; }
            }
        }

        if (fin < 0) return s.Substring(inicio); // Sin cierre; deja fallar al deserializer con texto desde '{'.

        return s.Substring(inicio, fin - inicio + 1);
    }

    // ConstruirPrompt y ParsearRespuesta del template method NO aplican en
    // este nodo (HandleAsync overridea el flujo entero). Las dejamos con
    // NotSupportedException explícita por si alguien las invocara
    // accidentalmente — el código actual no las llama.

    protected override string ConstruirPrompt(ContextoAuditoria input) =>
        throw new NotSupportedException(
            "DocumentAnalysisNode overridea HandleAsync por requerir I/O async " +
            "pre y post LLM. La construcción del prompt vive en " +
            "ConstruirPromptInterno y se invoca desde HandleAsync.");

    protected override DocumentosExtraidos ParsearRespuesta(
        ContextoAuditoria input, string textoLlm) =>
        throw new NotSupportedException(
            "DocumentAnalysisNode overridea HandleAsync. El parseo de la " +
            "respuesta del LLM vive en ParsearJsonLlm y se invoca desde HandleAsync.");
}


public sealed class ComplianceValidationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    private readonly IAuditoriaProgresoTracker _tracker;
    private readonly IRegistroErrorAuditoriaWriter _errorWriter;

    public ComplianceValidationNode(
        AIAgent agente,
        IAuditoriaProgresoTracker tracker,
        IRegistroErrorAuditoriaWriter errorWriter,
        ILogger<ComplianceValidationNode> logger)
        : base("ComplianceValidation", agente, logger)
    {
        _tracker = tracker;
        _errorWriter = errorWriter;
    }

    public override async ValueTask<HallazgosPreliminares> HandleAsync(
        DocumentosExtraidos message, IWorkflowContext context, CancellationToken ct = default)
    {
        await _tracker.MarcarEnCursoAsync(
            message.AuditoriaId, NodoWorkflow.ComplianceValidation, ct);

        try
        {
            var resultado = await base.HandleAsync(message, context, ct);
            await _tracker.MarcarCompletadoAsync(
                message.AuditoriaId, NodoWorkflow.ComplianceValidation, ct);
            return resultado;
        }
        catch (Exception ex)
        {
            await _errorWriter.RegistrarAsync(
                message.AuditoriaId, NodoWorkflow.ComplianceValidation, ex, CancellationToken.None);
            await _tracker.MarcarFallidoAsync(
                message.AuditoriaId, NodoWorkflow.ComplianceValidation, CancellationToken.None);
            throw;
        }
    }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
        => CompliancePromptBuilder.Construir(input);

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        var hallazgos = HallazgosDeterministicos.Generar(input);

        // Completitud del tailoring: responsable de la portada vacío → OBS (FR 29).
        hallazgos.AddRange(HallazgosResponsable.Generar(input));

        // Identidad del documento: código FR interno ≠ esperado, o proyecto interno
        // ≠ auditado → NC (evidencia equivocada).
        hallazgos.AddRange(HallazgosIdentidad.Generar(input));

        var idsExpuestos = input.Artefactos
            .Where(CompliancePromptBuilder.EsCandidatoLlm)
            .Select(a => a.ArtefactoEsperadoId)
            .ToHashSet();

        if (idsExpuestos.Count > 0)
        {
            var hallazgosLlm = ComplianceHallazgosPreliminaresParser.Parsear(
                textoLlm,
                idsExpuestos);

            hallazgos.AddRange(hallazgosLlm);
        }

        return new HallazgosPreliminares(
            input.AuditoriaId,
            AgenteOrigen.ComplianceValidation,
            hallazgos);
    }
}

// ============================================================================
//  [4] ConsistencyVerificationNode — agente con LLM. Contrato 3 INTACTO.
//  No valida firmas ni vigencia por fecha.
// ============================================================================

public sealed class ConsistencyVerificationNode
    : AgenteExecutorBase<DocumentosExtraidos, HallazgosPreliminares>
{
    private readonly IAuditoriaProgresoTracker _tracker;
    private readonly IRegistroErrorAuditoriaWriter _errorWriter;

    public ConsistencyVerificationNode(
        AIAgent agente,
        IAuditoriaProgresoTracker tracker,
        IRegistroErrorAuditoriaWriter errorWriter,
        ILogger<ConsistencyVerificationNode> logger)
        : base("ConsistencyVerification", agente, logger)
    {
        _tracker = tracker;
        _errorWriter = errorWriter;
    }

    public override async ValueTask<HallazgosPreliminares> HandleAsync(
        DocumentosExtraidos message, IWorkflowContext context, CancellationToken ct = default)
    {
        await _tracker.MarcarEnCursoAsync(
            message.AuditoriaId, NodoWorkflow.ConsistencyVerification, ct);

        try
        {
            var resultado = await base.HandleAsync(message, context, ct);
            await _tracker.MarcarCompletadoAsync(
                message.AuditoriaId, NodoWorkflow.ConsistencyVerification, ct);
            return resultado;
        }
        catch (Exception ex)
        {
            await _errorWriter.RegistrarAsync(
                message.AuditoriaId, NodoWorkflow.ConsistencyVerification, ex, CancellationToken.None);
            await _tracker.MarcarFallidoAsync(
                message.AuditoriaId, NodoWorkflow.ConsistencyVerification, CancellationToken.None);
            throw;
        }
    }

    protected override string ConstruirPrompt(DocumentosExtraidos input)
        => ConsistencyPromptBuilder.Construir(input);

    protected override HallazgosPreliminares ParsearRespuesta(
        DocumentosExtraidos input, string textoLlm)
    {
        // Fase 5: hallazgos estructurales determinísticos para artefactos con template.
        var conTemplate = input.Artefactos
            .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible
                     && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado
                     && a.SeccionesTemplate.Count > 0)
            .ToList();

        var hallazgosEstructurales = HallazgosEstructurales.Generar(
            conTemplate, input.ProcedimientoCodigo, input.ProcedimientoNombre);

        // Vigencia del formulario usado vs. la vigente en Calidad (determinístico,
        // OrigenRegla.Vigencia → OBS). Filtra internamente: Exigible + Encontrado +
        // parseable + con referencia cargada.
        var hallazgosVigencia = HallazgosVigencia.Generar(input.Artefactos);

        // Replicar el filtrado del prompt: el LLM vio los artefactos Exigibles +
        // Encontrados con al menos una sección vacía (mismo criterio que el builder).
        var idsExpuestos = input.Artefactos
            .Where(ConsistencyPromptBuilder.EsCandidatoLlm)
            .Select(a => a.ArtefactoEsperadoId)
            .ToHashSet();

        List<HallazgoPreliminar> hallazgosLlm;
        if (idsExpuestos.Count == 0)
        {
            hallazgosLlm = new List<HallazgoPreliminar>();
        }
        else
        {
            var parsed = HallazgosPreliminaresParser.Parsear(textoLlm, idsExpuestos, input.AuditoriaId);
            hallazgosLlm = parsed.Hallazgos.ToList();
        }

        var todos = new List<HallazgoPreliminar>(
            hallazgosEstructurales.Count + hallazgosVigencia.Count + hallazgosLlm.Count);
        todos.AddRange(hallazgosEstructurales);
        todos.AddRange(hallazgosVigencia);
        todos.AddRange(hallazgosLlm);

        // Dedup defensivo: misma clave que HallazgosPreliminaresParser.
        var sinDuplicados = todos
            .DistinctBy(h => (h.ArtefactoEsperadoId, h.Descripcion, h.Justificacion))
            .ToList();

        return new HallazgosPreliminares(
            input.AuditoriaId,
            AgenteOrigen.ConsistencyVerification,
            sinDuplicados);
    }
}

// ============================================================================
//  [5] FindingsClassificationNode — agente con LLM, TRES entradas cacheadas
// ============================================================================

// Declara que este executor emite ResultadoClasificacionConContexto.

[SendsMessage(typeof(ResultadoClasificacionConContexto))]
public sealed partial class FindingsClassificationNode : Executor
{
    private const int MaxIntentos = 3;

    private readonly AIAgent _agente;
    private readonly IAuditoriaProgresoTracker _tracker;
    private readonly IRegistroErrorAuditoriaWriter _errorWriter;
    private readonly ILogger<FindingsClassificationNode> _logger;

    // Los tres elementos a cachear antes de poder clasificar.
    private DocumentosExtraidos? _contextoDocumentos;
    private HallazgosPreliminares? _hallazgosCompliance;
    private HallazgosPreliminares? _hallazgosConsistency;

    public FindingsClassificationNode(
        AIAgent agente,
        IAuditoriaProgresoTracker tracker,
        IRegistroErrorAuditoriaWriter errorWriter,
        ILogger<FindingsClassificationNode> logger)
        : base("FindingsClassification")
    {
        _agente = agente;
        _tracker = tracker;
        _errorWriter = errorWriter;
        _logger = logger;
    }

    // --- Entrada A: contexto, por edge directo desde DocumentAnalysis -------
    [MessageHandler]
    private async ValueTask HandleContextoAsync(
        DocumentosExtraidos message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        if (_contextoDocumentos is not null)
        {
            throw new InvalidOperationException(
                "FindingsClassification recibió DocumentosExtraidos más de " +
                "una vez. Revisar el cableado del grafo.");
        }

        _contextoDocumentos = message;
        await IntentarClasificarAsync(context);
    }

    // --- Entrada B: cada lote de hallazgos, de a uno ------------------------
    [MessageHandler]
    private async ValueTask HandleHallazgosAsync(
        HallazgosPreliminares message,
        IWorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Distinguir el lote por su AgenteOrigen y cachearlo en su slot.
        switch (message.AgenteOrigen)
        {
            case AgenteOrigen.ComplianceValidation:
                if (_hallazgosCompliance is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ComplianceValidation. Se esperaba uno solo.");
                }
                _hallazgosCompliance = message;
                break;

            case AgenteOrigen.ConsistencyVerification:
                if (_hallazgosConsistency is not null)
                {
                    throw new InvalidOperationException(
                        "FindingsClassification recibió dos lotes de " +
                        "ConsistencyVerification. Se esperaba uno solo.");
                }
                _hallazgosConsistency = message;
                break;

            default:
                throw new InvalidOperationException(
                    $"AgenteOrigen no contemplado en el fan-in: " +
                    $"{message.AgenteOrigen}.");
        }

        await IntentarClasificarAsync(context);
    }

    // --- Disparo: clasifica cuando están los TRES elementos -----------------
    private async ValueTask IntentarClasificarAsync(IWorkflowContext context)
    {
        // Falta alguno de los tres -> esperar al próximo mensaje.
        if (_contextoDocumentos is null
            || _hallazgosCompliance is null
            || _hallazgosConsistency is null)
        {
            return;
        }

        // Aserción de invariante: los tres elementos de la misma auditoría.
        int idContexto = _contextoDocumentos.AuditoriaId;
        if (_hallazgosCompliance.AuditoriaId != idContexto
            || _hallazgosConsistency.AuditoriaId != idContexto)
        {
            throw new InvalidOperationException(
                "AuditoriaId inconsistente entre los lotes de hallazgos y el " +
                "contexto recibidos por FindingsClassification.");
        }

        var lotes = new[] { _hallazgosCompliance, _hallazgosConsistency };

        await _tracker.MarcarEnCursoAsync(
            idContexto, NodoWorkflow.FindingsClassification, CancellationToken.None);

        try
        {
            // 1. Prompt de clasificación (se construye una sola vez; el retry
            //    solo repite la llamada al LLM, no reconstruye el prompt).
            string prompt = ConstruirPrompt(lotes);

            // 2 + 3. El LLM clasifica con reintentos automáticos.
            // El LLM ocasionalmente devuelve JSON malformado (propiedad 'indice'
            // ausente, prosa mezclada, array truncado). Un reintento inmediato
            // suele obtener una respuesta válida sin cambiar el prompt.
            HallazgosClasificados clasificacion = await ClasificarConReintentosAsync(
                lotes, prompt, idContexto);

            // 5. Combinar con el contexto conservado (no pasó por el LLM).
            var salida = new ResultadoClasificacionConContexto(
                Clasificacion: clasificacion,
                ContextoDocumentos: _contextoDocumentos);

            await context.SendMessageAsync(salida, CancellationToken.None);

            await _tracker.MarcarCompletadoAsync(
                idContexto, NodoWorkflow.FindingsClassification, CancellationToken.None);
        }
        catch (Exception ex)
        {
            await _errorWriter.RegistrarAsync(
                idContexto, NodoWorkflow.FindingsClassification, ex, CancellationToken.None);
            await _tracker.MarcarFallidoAsync(
                idContexto, NodoWorkflow.FindingsClassification, CancellationToken.None);
            throw;
        }
        finally
        {
            // Higiene defensiva: liberar el estado cacheado.
            _contextoDocumentos = null;
            _hallazgosCompliance = null;
            _hallazgosConsistency = null;
        }
    }

    private async Task<HallazgosClasificados> ClasificarConReintentosAsync(
        IReadOnlyList<HallazgosPreliminares> lotes,
        string prompt,
        int idContexto)
    {
        Exception? ultimoError = null;

        for (int intento = 1; intento <= MaxIntentos; intento++)
        {
            try
            {
                var respuesta = await _agente.RunAsync(prompt);
                string textoLlm = respuesta.Text ?? string.Empty;
                return ParsearRespuesta(lotes, textoLlm);
            }
            catch (InvalidOperationException ex) when (intento < MaxIntentos)
            {
                ultimoError = ex;
                _logger.LogWarning(ex,
                    "FindingsClassification: intento {Intento}/{Max} falló al parsear " +
                    "respuesta del LLM (auditoría {Id}). Reintentando.",
                    intento, MaxIntentos, idContexto);
            }
        }

        throw ultimoError!;
    }

    private string ConstruirPrompt(IReadOnlyList<HallazgosPreliminares> hallazgos)
        => FindingsPromptBuilder.Construir(hallazgos, _contextoDocumentos!);

    private HallazgosClasificados ParsearRespuesta(
        IReadOnlyList<HallazgosPreliminares> hallazgos, string textoLlm)
    {
        // Aplanamos los lotes en la MISMA forma que ConstruirPrompt: misma lista,
        // mismo orden, mismos índices. El indice 0..N-1 es la identidad del
        // hallazgo para el LLM y para el parser.
        //
        // El AgenteOrigen vive en el raíz del lote (contrato 4); lo "bajamos" a
        // nivel de hallazgo individual para que el parser pueda emitir
        // HallazgoClasificado con su AgenteOrigen correcto.
        var preliminaresPlanos = hallazgos
            .SelectMany(lote => lote.Hallazgos
                .Select(h => (Hallazgo: h, Origen: lote.AgenteOrigen)))
            .ToList();

        // AuditoriaId ya validado en IntentarClasificarAsync — los tres lotes
        // comparten Id. Tomamos el primero por conveniencia.
        var auditoriaId = hallazgos[0].AuditoriaId;

        return ClasificacionResponseParser.Parsear(textoLlm, preliminaresPlanos, auditoriaId);
    }

    // ------------------------------------------------------------------------
    //  MAF: registro explícito del protocolo para el nodo con múltiples entradas.
    //  FindingsClassification recibe:
    //   - DocumentosExtraidos
    //   - HallazgosPreliminares
    //
    //  También declara que emite ResultadoClasificacionConContexto.
    //  Esto evita depender del ruteo implícito por atributos.
    // ------------------------------------------------------------------------
    protected override ProtocolBuilder ConfigureProtocol(ProtocolBuilder protocolBuilder)
    {
        return protocolBuilder
            .SendsMessage<ResultadoClasificacionConContexto>()
            .ConfigureRoutes(routes =>
            {
                routes.AddHandler<DocumentosExtraidos>(HandleContextoAsync);
                routes.AddHandler<HallazgosPreliminares>(HandleHallazgosAsync);
            });
    }
}

// ============================================================================
//  [6] ConsolidadorResultadoNode — nodo determinista final, sin LLM
// ----------------------------------------------------------------------------

public sealed class ConsolidadorResultadoNode
    : Executor<ResultadoClasificacionConContexto, AuditoriaResultado>
{
    public ConsolidadorResultadoNode() : base("ConsolidadorResultado") { }

    public override ValueTask<AuditoriaResultado> HandleAsync(
        ResultadoClasificacionConContexto message,
        IWorkflowContext context,
        CancellationToken ct = default)
    {
        var resultado = ConsolidadorEnsamble.Ensamblar(
            message.ContextoDocumentos, message.Clasificacion);
        return ValueTask.FromResult(resultado);
    }
}
