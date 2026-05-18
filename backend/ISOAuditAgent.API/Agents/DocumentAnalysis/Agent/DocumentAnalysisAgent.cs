using System.Text.Json;
using System.Text.Json.Serialization;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis;
using ISOAuditAgent.DocumentAnalysis.Agent.Configuration;
using ISOAuditAgent.DocumentAnalysis.Agent.Models;
using ISOAuditAgent.DocumentAnalysis.Agent.Prompts;
using ISOAuditAgent.DocumentAnalysis.Agent.Tools;
using ISOAuditAgent.DocumentAnalysis.Agent.Validation;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.DocumentAnalysis.Agent;

public sealed class DocumentAnalysisAgent : IDocumentAnalysisAgent
{
    private static readonly JsonSerializerOptions JsonSerializerCamel = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true
    };

    private readonly IContextoAuditoriaService _contexto;
    private readonly TailoringTool _tailoring;
    private readonly VerificarArtefactoTool _verificar;
    private readonly IChatClient _chat;
    private readonly DocumentAnalysisAgentOptions _options;
    private readonly ILogger<DocumentAnalysisAgent> _logger;

    public DocumentAnalysisAgent(
        IContextoAuditoriaService contexto,
        TailoringTool tailoring,
        VerificarArtefactoTool verificar,
        IChatClient chat,
        IOptions<DocumentAnalysisAgentOptions> options,
        ILogger<DocumentAnalysisAgent>? logger = null)
    {
        _contexto = contexto ?? throw new ArgumentNullException(nameof(contexto));
        _tailoring = tailoring ?? throw new ArgumentNullException(nameof(tailoring));
        _verificar = verificar ?? throw new ArgumentNullException(nameof(verificar));
        _chat = chat ?? throw new ArgumentNullException(nameof(chat));
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger ?? NullLogger<DocumentAnalysisAgent>.Instance;
    }

    public async Task<DocumentosExtraidos> ExecuteAsync(
        DocumentAnalysisRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.AuditoriaId <= 0 || request.ProyectoId <= 0 || request.EtapaId <= 0)
        {
            throw new ArgumentException(
                "AuditoriaId, ProyectoId y EtapaId deben ser > 0.",
                nameof(request));
        }

        var contexto = await _contexto
            .GetContextoAuditoriaAsync(request.ProyectoId, request.EtapaId, cancellationToken)
            .ConfigureAwait(false);

        var tailoring = await _tailoring
            .GetTailoringAsync(request.ProyectoId, cancellationToken)
            .ConfigureAwait(false);

        var payload = new
        {
            nota = "contexto y tailoring ya obtenidos en servidor; no invoques tools. Emití solo el JSON final del paso 5.",
            contexto,
            tailoring
        };

        var userJson = JsonSerializer.Serialize(payload, JsonSerializerCamel);

        var messages = new List<ChatMessage>
        {
            new(ChatRole.System, SystemPromptBuilder.Build()),
            new(ChatRole.User, userJson)
        };

        var chatOptions = new ChatOptions
        {
            Temperature = _options.TemperatureLow,
            ResponseFormat = ChatResponseFormat.Json
        };

        LlmOutput? llm = null;
        var maxAttempts = Math.Max(1, _options.MaxLlmRetries + 1);
        Exception? lastError = null;

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await _chat
                    .GetResponseAsync(messages, chatOptions, cancellationToken)
                    .ConfigureAwait(false);

                var text = response.Text?.Trim();
                if (string.IsNullOrEmpty(text))
                {
                    throw new InvalidOperationException(
                        "El LLM devolvió una respuesta vacía.");
                }

                llm = JsonSerializer.Deserialize<LlmOutput>(text, JsonSerializerCamel);
                if (llm is null || llm.Artefactos is null || llm.Artefactos.Count == 0)
                {
                    throw new InvalidOperationException("JSON del LLM vacío o inválido.");
                }

                ValidateLlmAgainstContext(llm, contexto);

                var artefactos = await BuildArtefactosAsync(
                    request,
                    contexto,
                    llm,
                    cancellationToken).ConfigureAwait(false);

                var dto = new DocumentosExtraidos(
                    request.AuditoriaId,
                    request.ProyectoId,
                    request.EtapaId,
                    artefactos);

                InvariantsValidator.Validate(dto);
                return dto;
            }
            catch (Exception ex) when (ex is InvariantViolationException or InvalidOperationException
                                         or JsonException)
            {
                lastError = ex;
                _logger.LogWarning(ex, "Intento {Attempt}/{Max} del LLM / validación falló.",
                    attempt, maxAttempts);
            }
        }

        throw new InvalidOperationException(
            $"DocumentAnalysisAgent: se agotaron reintentos ({maxAttempts}).",
            lastError);
    }

    private static void ValidateLlmAgainstContext(LlmOutput llm, ProyectoContexto contexto)
    {
        var expectedIds = contexto.ArtefactosEsperados.Select(a => a.ArtefactoEsperadoId).OrderBy(x => x).ToArray();
        var gotIds = llm.Artefactos.Select(x => x.ArtefactoEsperadoId).OrderBy(x => x).ToArray();

        if (expectedIds.Length != gotIds.Length ||
            !expectedIds.SequenceEqual(gotIds))
        {
            throw new InvalidOperationException(
                "La lista artefactoEsperadoId del LLM no coincide con el contexto.");
        }
    }

    private async Task<IReadOnlyList<ArtefactoExtraido>> BuildArtefactosAsync(
        DocumentAnalysisRequest request,
        ProyectoContexto contexto,
        LlmOutput llm,
        CancellationToken cancellationToken)
    {
        var byId = llm.Artefactos.ToDictionary(x => x.ArtefactoEsperadoId);
        var result = new List<ArtefactoExtraido>();

        foreach (var view in contexto.ArtefactosEsperados)
        {
            if (!byId.TryGetValue(view.ArtefactoEsperadoId, out var row))
            {
                throw new InvalidOperationException(
                    $"Falta fila LLM para artefacto {view.ArtefactoEsperadoId}.");
            }

            var estadoTailoring = ParseEstadoTailoring(row.EstadoTailoring);

            string? justificacion = estadoTailoring == EstadoTailoring.NoAplica
                ? (string.IsNullOrWhiteSpace(row.JustificacionNoAplica)
                    ? null
                    : row.JustificacionNoAplica.Trim())
                : null;

            string? urlRef = string.IsNullOrWhiteSpace(row.UrlReferenciaTailoring)
                ? null
                : row.UrlReferenciaTailoring.Trim();

            EstadoDisponibilidad disponibilidad;
            DocumentoEncontrado? doc;
            IReadOnlyList<SeccionDetectada> secciones;
            string? pathTemplate = null;

            if (view.Exigibilidad == ExigibilidadArtefacto.PendienteEtapaFutura
                || estadoTailoring is EstadoTailoring.NoAplica or EstadoTailoring.SinDeclararEnTailoring)
            {
                disponibilidad = EstadoDisponibilidad.NoBuscado;
                doc = null;
                secciones = Array.Empty<SeccionDetectada>();
            }
            else if (estadoTailoring == EstadoTailoring.Aplica
                     && view.Exigibilidad == ExigibilidadArtefacto.Exigible)
            {
                var ver = await _verificar.VerificarArtefactoAsync(
                    request.ProyectoId,
                    view.ArtefactoEsperadoId,
                    urlRef,
                    view.Codigo,
                    view.TemplateDriveFilename,
                    contexto.DriveFolderIdTemplates,
                    cancellationToken).ConfigureAwait(false);

                pathTemplate = ver.TemplateFileIdResuelto;

                if (ver.Encontrado && ver.HashContenido is not null)
                {
                    disponibilidad = EstadoDisponibilidad.Encontrado;
                    doc = new DocumentoEncontrado(
                        ver.NombreArchivo ?? "sin-nombre",
                        FuenteDocumento.Drive,
                        ver.HashContenido);
                    secciones = ver.Secciones;
                }
                else
                {
                    disponibilidad = EstadoDisponibilidad.Faltante;
                    doc = null;
                    secciones = Array.Empty<SeccionDetectada>();
                }
            }
            else
            {
                disponibilidad = EstadoDisponibilidad.NoBuscado;
                doc = null;
                secciones = Array.Empty<SeccionDetectada>();
            }

            result.Add(new ArtefactoExtraido(
                view.ArtefactoEsperadoId,
                view.Codigo,
                view.Nombre,
                view.EtapaId,
                view.Exigibilidad,
                view.Obligatoriedad,
                estadoTailoring,
                justificacion,
                disponibilidad,
                urlRef,
                pathTemplate,
                doc,
                secciones));
        }

        return result;
    }

    private static EstadoTailoring ParseEstadoTailoring(string s)
    {
        var t = s.Trim();
        if (string.Equals(t, "Aplica", StringComparison.OrdinalIgnoreCase))
            return EstadoTailoring.Aplica;
        if (string.Equals(t, "NoAplica", StringComparison.OrdinalIgnoreCase))
            return EstadoTailoring.NoAplica;
        if (string.Equals(t, "SinDeclararEnTailoring", StringComparison.OrdinalIgnoreCase))
            return EstadoTailoring.SinDeclararEnTailoring;

        throw new InvalidOperationException($"estadoTailoring no reconocido: '{s}'.");
    }
}
