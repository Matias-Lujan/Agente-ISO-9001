using ISOAuditAgent.API.Agents.ConsistencyVerification.Contracts;
using ISOAuditAgent.API.Integrations.MCP;
using ISOAuditAgent.API.Models;
using ISOAuditAgent.API.Services;

namespace ISOAuditAgent.API.Agents.ConsistencyVerification;

/// <summary>
/// Agente de Verificacion de Consistencia — Contrato 3
///
/// RESPONSABILIDAD:
/// Detectar problemas de estructura y consistencia en los documentos
/// que DocumentAnalysis ya encontro y clasifico.
///
/// QUE HACE:
/// - Verifica que los documentos tengan las secciones requeridas con contenido
/// - Detecta inconsistencias entre documentos relacionados
///
/// QUE NO HACE (aclarado por el cliente de BDT):
/// - NO valida fechas ni vigencia (fecha pasada no significa documento vencido)
/// - NO valida firmas (fuera de alcance)
/// - NO clasifica NC/OBS/OM (eso le corresponde a FindingsClassification)
/// - NO busca documentos (eso ya lo hizo DocumentAnalysis)
///
/// ENTRADA:  DocumentosExtraidos (Contrato 2)
/// SALIDA:   HallazgosPreliminares (Contrato 3)
/// </summary>
public class ConsistencyVerificationAgentService
{
    private readonly IAiClient _ai;
    private readonly IDocumentSummaryBuilder _summaryBuilder;
    private readonly ILogger<ConsistencyVerificationAgentService> _logger;

    public ConsistencyVerificationAgentService(
        IAiClient ai,
        IDocumentSummaryBuilder summaryBuilder,
        ILogger<ConsistencyVerificationAgentService> logger)
    {
        _ai = ai;
        _summaryBuilder = summaryBuilder;
        _logger = logger;
    }

    // ── Entry point — llamado por el Controller o el orquestador ─────────────

    public async Task<HallazgosPreliminares> ExecuteAsync(
        DocumentosExtraidos input,
        CancellationToken ct = default)
    {
        _logger.LogInformation(
            "ConsistencyVerification iniciado | AuditoriaId={Id} | Artefactos={Count}",
            input.AuditoriaId, input.Artefactos.Count);

        try
        {
            // Solo analizamos artefactos Exigibles y Encontrados.
            // Si no esta, DocumentAnalysis ya lo marco como Faltante — no es nuestro trabajo.
            var analizables = input.Artefactos
                .Where(a => a.Exigibilidad == ExigibilidadArtefacto.Exigible
                         && a.EstadoDisponibilidad == EstadoDisponibilidad.Encontrado)
                .ToList();

            _logger.LogInformation(
                "Artefactos a analizar: {Count} de {Total}",
                analizables.Count, input.Artefactos.Count);

            // Lista vacia es valida segun el Contrato 3
            if (analizables.Count == 0)
            {
                _logger.LogInformation("No hay artefactos analizables. Devolviendo lista vacia.");
                return Vacio(input.AuditoriaId);
            }

            // Construimos el texto que le mandamos a Gemini
            var resumen = _summaryBuilder.BuildArtefactosSummary(input.Artefactos);

            _logger.LogInformation("Enviando analisis a Gemini...");

            var rawResponse = await _ai.CompleteAsync(
                systemPrompt: "Eres un auditor ISO 9001 experto en procesos de desarrollo de software. " +
                              "Responde SIEMPRE en JSON valido, sin texto adicional.",
                userMessage: BuildPrompt(resumen),
                ct);

            // Convertimos la respuesta de Gemini al formato del Contrato 3
            var aiResponse = AiResponseParser.ParseJson<GeminiHallazgosResponse>(rawResponse, _logger);
            var hallazgos = MapHallazgos(aiResponse, input.Artefactos);

            _logger.LogInformation(
                "ConsistencyVerification completado | AuditoriaId={Id} | Hallazgos={Count}",
                input.AuditoriaId, hallazgos.Count);

            return new HallazgosPreliminares(
                input.AuditoriaId,
                AgenteOrigen.ConsistencyVerification,
                hallazgos);
        }
        catch (Exception ex)
        {
            // Si falla, devolvemos lista vacia para no bloquear el workflow.
            // El orquestador decide como manejar esto.
            _logger.LogError(ex,
                "ConsistencyVerification fallo | AuditoriaId={Id}", input.AuditoriaId);
            return Vacio(input.AuditoriaId);
        }
    }

    // ── Helpers privados ──────────────────────────────────────────────────────

    private static HallazgosPreliminares Vacio(int auditoriaId) =>
        new(auditoriaId, AgenteOrigen.ConsistencyVerification, Array.Empty<HallazgoPreliminar>());

    // ── Construccion del prompt ───────────────────────────────────────────────

    private static string BuildPrompt(string resumenArtefactos) =>
        "Sos un auditor experto en ISO 9001 y en el procedimiento PR 11-13 de BDT Global.\n\n" +
        "Analiza los siguientes artefactos de un proyecto de desarrollo de software " +
        "y detecta problemas de estructura y consistencia.\n\n" +
        "ARTEFACTOS A ANALIZAR:\n" +
        resumenArtefactos + "\n\n" +
        "QUE DEBES DETECTAR:\n" +
        "1. Secciones que existen pero estan vacias y deberian tener contenido.\n" +
        "2. Inconsistencias entre artefactos relacionados.\n" +
        "3. Contenido insuficiente para el proposito del artefacto.\n\n" +
        "QUE NO DEBES VALIDAR (fuera de alcance segun el cliente):\n" +
        "- Fechas o vigencia. Una fecha pasada NO es un problema.\n" +
        "- Firmas o aprobaciones.\n" +
        "- Si el documento existe o no (eso ya lo evaluo DocumentAnalysis).\n\n" +
        "Solo genera hallazgos sobre artefactos con problemas reales. " +
        "Si un artefacto esta bien, no lo incluyas.\n\n" +
        "Responde UNICAMENTE con este JSON (sin texto antes ni despues):\n" +
        "{\n" +
        "  \"hallazgos\": [\n" +
        "    {\n" +
        "      \"artefactoEsperadoId\": 0,\n" +
        "      \"descripcion\": \"descripcion clara del problema\",\n" +
        "      \"justificacion\": \"por que es un problema segun el procedimiento o template\",\n" +
        "      \"origenRegla\": \"Procedimiento | Template | Tailoring\"\n" +
        "    }\n" +
        "  ]\n" +
        "}";

    // ── Mapeo de la respuesta de Gemini al Contrato 3 ────────────────────────

    private List<HallazgoPreliminar> MapHallazgos(
        GeminiHallazgosResponse? response,
        IReadOnlyList<ArtefactoExtraido> artefactos)
    {
        if (response?.Hallazgos == null || response.Hallazgos.Count == 0)
            return [];

        // Solo aceptamos hallazgos sobre artefactos que realmente existen en el input
        // (invariante del Contrato 3)
        var idsValidos = artefactos.Select(a => a.ArtefactoEsperadoId).ToHashSet();
        var resultado = new List<HallazgoPreliminar>();

        foreach (var h in response.Hallazgos)
        {
            // Validar que referencia un artefacto real
            if (!idsValidos.Contains(h.ArtefactoEsperadoId))
            {
                _logger.LogWarning(
                    "Hallazgo con ArtefactoEsperadoId={Id} no existe en el input. Se descarta.",
                    h.ArtefactoEsperadoId);
                continue;
            }

            // Descripcion y Justificacion son requeridos (invariante del Contrato 3)
            if (string.IsNullOrWhiteSpace(h.Descripcion) ||
                string.IsNullOrWhiteSpace(h.Justificacion))
            {
                _logger.LogWarning(
                    "Hallazgo con ArtefactoEsperadoId={Id} tiene campos vacios. Se descarta.",
                    h.ArtefactoEsperadoId);
                continue;
            }

            var origenRegla = h.OrigenRegla?.ToLower() switch
            {
                "template"  => OrigenRegla.Template,
                "tailoring" => OrigenRegla.Tailoring,
                _           => OrigenRegla.Procedimiento
            };

            resultado.Add(new HallazgoPreliminar(
                h.ArtefactoEsperadoId,
                h.Descripcion.Trim(),
                h.Justificacion.Trim(),
                origenRegla));
        }

        _logger.LogInformation(
            "Hallazgos validos: {Count} de {Total} generados por Gemini",
            resultado.Count, response.Hallazgos.Count);

        return resultado;
    }

    // ── Tipos internos para deserializar la respuesta de Gemini ──────────────
    // Privados — son un detalle de implementacion, no parte del contrato.

    private record GeminiHallazgosResponse(List<GeminiHallazgo>? Hallazgos);

    private record GeminiHallazgo(
        int ArtefactoEsperadoId,
        string? Descripcion,
        string? Justificacion,
        string? OrigenRegla);
}
