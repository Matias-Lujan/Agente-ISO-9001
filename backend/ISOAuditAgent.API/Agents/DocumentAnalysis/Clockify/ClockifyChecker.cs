// ============================================================================
//  ClockifyChecker — Verifica evidencia de artefactos en Clockify (D6.Clockify)
// ----------------------------------------------------------------------------
//  El project ID se extrae de la URL del tailoring (urlRef) por dos estrategias:
//
//  ESTRATEGIA 1 — path /projects/{24hex}:
//   Regex: /projects/([a-fA-F0-9]{24})(?:[/?#]|$)
//   Ejemplo: https://app.clockify.me/projects/64a1b2c3d4e5f6a7b8c9d0e1/edit
//
//  ESTRATEGIA 2 — query param filterValuesData (URL de reports/summary):
//   URL: .../reports/summary?filterValuesData=%7B%22projects%22:%5B%2269e397...%22%5D%7D
//   Decodifica filterValuesData → JSON → projects[0] (ObjectId 24 hex).
//   Ejemplo real: https://app.clockify.me/reports/summary?filterValuesData=...
//
//  Si ninguna estrategia extrae un ID válido → NoEncontrado (NC via Compliance).
//
//  Criterio de conformidad: el proyecto debe existir (presencia).
//  NOTA: la llamada a reports.api.clockify.me para horas está DESHABILITADA
//  temporalmente para diagnóstico. Reactivar cuando el pipeline base funcione.
//
//  Resiliencia: cualquier fallo de API → NoEncontrado + log warning.
// ============================================================================

using System.Text.Json;
using System.Text.RegularExpressions;
using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Integrations.MCP.Clockify;
using ISOAuditAgent.API.Models;
using Microsoft.Extensions.Options;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Clockify;

public sealed class ClockifyChecker
{
    // Estrategia 1: /projects/{24hex} en el path.
    private static readonly Regex ProjectIdPathRegex = new(
        @"/projects/([a-fA-F0-9]{24})(?:[/?#]|$)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

    private static readonly VerificacionFisica NoEncontrado = new(
        Encontrado: false,
        Fuente: FuenteDocumento.Clockify,
        NombreArchivo: null,
        HashContenido: null,
        Secciones: Array.Empty<SeccionDetectada>(),
        SeccionesTemplate: Array.Empty<SeccionDetectada>(),
        NombreTemplateArchivo: null);

    private readonly IClockifyMcpClient _clockify;
    private readonly ClockifyOptions _opts;
    private readonly ILogger<ClockifyChecker> _logger;

    public ClockifyChecker(
        IClockifyMcpClient clockify,
        IOptions<ClockifyOptions> opts,
        ILogger<ClockifyChecker> logger)
    {
        _clockify = clockify;
        _opts = opts.Value;
        _logger = logger;
    }

    public async ValueTask<VerificacionFisica> VerificarAsync(string? urlRef, CancellationToken ct)
    {
        _logger.LogInformation("ClockifyChecker: iniciando verificación. urlRef='{Url}'.", urlRef ?? "(null)");

        var projectId = ParseProjectId(urlRef);
        _logger.LogInformation("ClockifyChecker: projectId extraído='{ProjectId}'.", projectId ?? "(null)");

        if (projectId is null)
        {
            _logger.LogInformation(
                "ClockifyChecker: no se pudo extraer project ID de la URL. " +
                "Estrategias intentadas: /projects/<24hex> en path, filterValuesData en query. " +
                "Devuelvo NoEncontrado.");
            return NoEncontrado;
        }

        _logger.LogInformation("ClockifyChecker: workspaceId='{WsId}'.", _opts.WorkspaceId);

        if (string.IsNullOrWhiteSpace(_opts.WorkspaceId))
        {
            _logger.LogWarning("ClockifyChecker: Clockify:WorkspaceId no configurado. Devuelvo NoEncontrado.");
            return NoEncontrado;
        }

        try
        {
            _logger.LogInformation(
                "ClockifyChecker: llamando GET /workspaces/{WsId}/projects/{ProjId}.",
                _opts.WorkspaceId, projectId);

            var project = await _clockify.GetProjectAsync(_opts.WorkspaceId, projectId, ct)
                .ConfigureAwait(false);

            _logger.LogInformation(
                "ClockifyChecker: GET project → '{Resultado}'.",
                project is null ? "null (404)" : $"OK nombre='{project.Name}' archived={project.Archived}");

            if (project is null)
            {
                _logger.LogInformation(
                    "ClockifyChecker: proyecto {ProjectId} no encontrado. Devuelvo NoEncontrado.", projectId);
                return NoEncontrado;
            }

            // TODO (DIAGNÓSTICO): la llamada a reports.api.clockify.me para horas está
            // deshabilitada temporalmente. Reactivar cuando el pipeline base funcione.
            // Criterio completo: proyecto existe + horas > 0.
            _logger.LogInformation(
                "ClockifyChecker: proyecto '{Nombre}' ({ProjectId}) encontrado OK. " +
                "Devuelvo Encontrado (verificación de horas pendiente).",
                project.Name, projectId);

            return new VerificacionFisica(
                Encontrado: true,
                Fuente: FuenteDocumento.Clockify,
                NombreArchivo: project.Name,
                HashContenido: string.Empty,
                Secciones: Array.Empty<SeccionDetectada>(),
                SeccionesTemplate: Array.Empty<SeccionDetectada>(),
                NombreTemplateArchivo: null);
        }
        catch (OperationCanceledException ex)
        {
            _logger.LogWarning(ex,
                "ClockifyChecker: timeout verificando proyecto {ProjectId}. Devuelvo NoEncontrado.", projectId);
            return NoEncontrado;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "ClockifyChecker: error verificando proyecto {ProjectId} " +
                "(auth inválida, rate-limit o red caída). Tipo={ExType}. Devuelvo NoEncontrado.",
                projectId, ex.GetType().Name);
            return NoEncontrado;
        }
    }

    /// <summary>
    /// Extrae el project ID de una URL de Clockify.
    /// Estrategia 1: /projects/{24hex} en el path.
    /// Estrategia 2: filterValuesData query param (URLs de /reports/summary).
    /// </summary>
    internal static string? ParseProjectId(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;

        // Estrategia 1: /projects/{24hex} en el path
        var m = ProjectIdPathRegex.Match(url);
        if (m.Success) return m.Groups[1].Value;

        // Estrategia 2: filterValuesData={"projects":["<id>",...]}
        return ParseFromFilterValuesData(url);
    }

    private static string? ParseFromFilterValuesData(string url)
    {
        try
        {
            var qMark = url.IndexOf('?');
            if (qMark < 0) return null;

            var query = url.Substring(qMark + 1);
            const string key = "filterValuesData=";
            var idx = query.IndexOf(key, StringComparison.OrdinalIgnoreCase);
            if (idx < 0) return null;

            var encoded = query.Substring(idx + key.Length);
            // Recortar en el siguiente & si lo hay
            var amp = encoded.IndexOf('&');
            if (amp >= 0) encoded = encoded.Substring(0, amp);

            var decoded = Uri.UnescapeDataString(encoded);

            // Parsear JSON: {"projects":["<id>", ...]}
            using var doc = JsonDocument.Parse(decoded);
            if (!doc.RootElement.TryGetProperty("projects", out var projects)) return null;
            if (projects.ValueKind != JsonValueKind.Array) return null;

            foreach (var item in projects.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) continue;
                var id = item.GetString();
                if (IsValidObjectId(id)) return id;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private static bool IsValidObjectId(string? id) =>
        id is { Length: 24 } &&
        id.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F');
}
