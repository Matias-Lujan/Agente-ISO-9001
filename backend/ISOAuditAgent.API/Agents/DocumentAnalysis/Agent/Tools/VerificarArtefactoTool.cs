using System.ComponentModel;
using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Agent.Models;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Agent.Tools;

/// <summary>
/// Descarga y parsea artefactos desde Drive vía MCP (tool
/// <c>verificar_artefacto_en_drive</c> del handoff).
/// </summary>
public sealed class VerificarArtefactoTool
{
    private readonly IDriveMcpClient _mcp;
    private readonly DocumentParserRegistry _parsers;
    private readonly ILogger<VerificarArtefactoTool> _logger;

    public VerificarArtefactoTool(
        IDriveMcpClient mcp,
        DocumentParserRegistry parsers,
        ILogger<VerificarArtefactoTool>? logger = null)
    {
        _mcp = mcp ?? throw new ArgumentNullException(nameof(mcp));
        _parsers = parsers ?? throw new ArgumentNullException(nameof(parsers));
        _logger = logger ?? NullLogger<VerificarArtefactoTool>.Instance;
    }

    [Description(
        "Para un artefacto exigible que aplica: descarga el documento desde Drive usando la URL del tailoring o el nombre en la carpeta del proyecto.")]
    public async Task<VerificacionArtefacto> VerificarArtefactoAsync(
        [Description("Id del proyecto")] int proyectoId,
        [Description("Id del artefacto esperado (correlación)")] int artefactoEsperadoId,
        [Description("URL en Drive declarada en el tailoring; puede ser null")]
        string? urlReferencia,
        [Description("Nombre esperado del archivo; puede ser null")] string? nombreEsperado,
        [Description("Nombre del archivo de template en la carpeta de templates")]
        string? templateDriveFilename,
        [Description("FolderId de la carpeta de templates; puede ser null")] string? driveFolderIdTemplates,
        CancellationToken cancellationToken = default)
    {
        _ = artefactoEsperadoId;
        _ = templateDriveFilename;
        _ = driveFolderIdTemplates;

        DriveFileContent? content = null;

        if (IsGoogleDriveUrl(urlReferencia))
        {
            try
            {
                content = await _mcp
                    .GetFileContentByDriveUrlAsync(urlReferencia!, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo descargar por URL de Drive para artefacto {Id}.",
                    artefactoEsperadoId);
            }
        }

        if (content is null && !string.IsNullOrWhiteSpace(nombreEsperado))
        {
            var listing = await _mcp
                .ListProjectFilesAsync(proyectoId, cancellationToken)
                .ConfigureAwait(false);

            var match = listing.Files.FirstOrDefault(f =>
                string.Equals(f.Name, nombreEsperado, StringComparison.OrdinalIgnoreCase));

            if (match is not null)
            {
                content = await _mcp
                    .GetFileContentAsync(match.Id, cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        // Fase F: sin list_files_under_folder en MCP, PathTemplateAbsoluto queda null (deuda técnica).
        string? templateFileId = null;

        if (content is null)
        {
            return new VerificacionArtefacto(
                Encontrado: false,
                NombreArchivo: null,
                HashContenido: null,
                Secciones: Array.Empty<SeccionDetectada>(),
                TemplateFileIdResuelto: templateFileId,
                ErrorMensaje: "No se localizó el artefacto en Drive.");
        }

        if (!_parsers.TryGetParser(content.MimeType, out var parser) || parser is null)
        {
            return new VerificacionArtefacto(
                Encontrado: false,
                NombreArchivo: content.Name,
                HashContenido: null,
                Secciones: Array.Empty<SeccionDetectada>(),
                TemplateFileIdResuelto: templateFileId,
                ErrorMensaje: $"MIME no soportado: '{content.MimeType}'.");
        }

        try
        {
            await using var stream = new MemoryStream(content.Bytes, writable: false);
            var parseResult = await parser
                .ParseAsync(stream, content.Name, cancellationToken)
                .ConfigureAwait(false);

            var hash = ContentHasher.ComputeSha256OfBytes(content.Bytes);

            return new VerificacionArtefacto(
                Encontrado: true,
                NombreArchivo: content.Name,
                HashContenido: hash,
                Secciones: parseResult.Secciones,
                TemplateFileIdResuelto: templateFileId,
                ErrorMensaje: null);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error parseando artefacto {Id}.", artefactoEsperadoId);
            return new VerificacionArtefacto(
                Encontrado: false,
                NombreArchivo: content.Name,
                HashContenido: null,
                Secciones: Array.Empty<SeccionDetectada>(),
                TemplateFileIdResuelto: templateFileId,
                ErrorMensaje: ex.Message);
        }
    }

    private static bool IsGoogleDriveUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.StartsWith("https://drive.google.com", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://docs.google.com", StringComparison.OrdinalIgnoreCase);
    }
}
