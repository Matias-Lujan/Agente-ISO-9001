using System.ComponentModel;
using System.Text.RegularExpressions;
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
/// <c>verificar_artefacto_en_drive</c> del handoff). Resuelve también el
/// FileId del template en la carpeta de templates del sistema para poblar
/// <c>PathTemplateAbsoluto</c> del Contrato 2.
/// </summary>
public sealed partial class VerificarArtefactoTool
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
        "Para un artefacto exigible que aplica: descarga el documento desde Drive " +
        "intentando primero la URL del tailoring, luego búsqueda por nombre o código " +
        "en la carpeta del proyecto. Resuelve también el FileId del template.")]
    public async Task<VerificacionArtefacto> VerificarArtefactoAsync(
        [Description("Id del proyecto")] int proyectoId,
        [Description("Id del artefacto esperado (correlación)")] int artefactoEsperadoId,
        [Description("URL en Drive declarada en el tailoring; puede ser null")]
        string? urlReferencia,
        [Description("Código del artefacto esperado (p. ej. 'FR 30'); usado como fallback de búsqueda")]
        string? codigoArtefacto,
        [Description("Nombre del archivo de template en la carpeta de templates")]
        string? templateDriveFilename,
        [Description("FolderId de la carpeta de templates; puede ser null")] string? driveFolderIdTemplates,
        CancellationToken cancellationToken = default)
    {
        var content = await ResolverDocumentoAsync(
            proyectoId,
            artefactoEsperadoId,
            urlReferencia,
            codigoArtefacto,
            templateDriveFilename,
            cancellationToken).ConfigureAwait(false);

        var templateFileId = await ResolverTemplateFileIdAsync(
            templateDriveFilename,
            driveFolderIdTemplates,
            cancellationToken).ConfigureAwait(false);

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

    private async Task<DriveFileContent?> ResolverDocumentoAsync(
        int proyectoId,
        int artefactoEsperadoId,
        string? urlReferencia,
        string? codigoArtefacto,
        string? templateDriveFilename,
        CancellationToken cancellationToken)
    {
        if (IsGoogleDriveUrl(urlReferencia))
        {
            try
            {
                return await _mcp
                    .GetFileContentByDriveUrlAsync(urlReferencia!, cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex,
                    "No se pudo descargar por URL de Drive para artefacto {Id}; intento fallback por nombre.",
                    artefactoEsperadoId);
            }
        }

        var hayNombre = !string.IsNullOrWhiteSpace(templateDriveFilename);
        var hayCodigo = !string.IsNullOrWhiteSpace(codigoArtefacto);
        if (!hayNombre && !hayCodigo)
            return null;

        DriveFileListing listing;
        try
        {
            listing = await _mcp
                .ListProjectFilesAsync(proyectoId, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo listar la carpeta del proyecto {ProyectoId} para artefacto {Id}.",
                proyectoId, artefactoEsperadoId);
            return null;
        }

        var match = BuscarMejorMatch(listing.Files, templateDriveFilename, codigoArtefacto);
        if (match is null)
            return null;

        try
        {
            return await _mcp
                .GetFileContentAsync(match.Id, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error descargando archivo '{Name}' ({FileId}) para artefacto {Id}.",
                match.Name, match.Id, artefactoEsperadoId);
            return null;
        }
    }

    private async Task<string?> ResolverTemplateFileIdAsync(
        string? templateDriveFilename,
        string? driveFolderIdTemplates,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(templateDriveFilename)
            || string.IsNullOrWhiteSpace(driveFolderIdTemplates))
            return null;

        DriveFolderListing listing;
        try
        {
            listing = await _mcp
                .ListFilesUnderFolderAsync(driveFolderIdTemplates, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "No se pudo listar la carpeta de templates '{FolderId}'.",
                driveFolderIdTemplates);
            return null;
        }

        var match = listing.Files.FirstOrDefault(f =>
            string.Equals(f.Name, templateDriveFilename, StringComparison.OrdinalIgnoreCase));

        if (match is not null)
            return match.Id;

        var sinExt = StripExtension(templateDriveFilename);
        match = listing.Files.FirstOrDefault(f =>
            string.Equals(StripExtension(f.Name), sinExt, StringComparison.OrdinalIgnoreCase));

        return match?.Id;
    }

    private static DriveFile? BuscarMejorMatch(
        IReadOnlyList<DriveFile> files,
        string? templateDriveFilename,
        string? codigoArtefacto)
    {
        if (files.Count == 0) return null;

        if (!string.IsNullOrWhiteSpace(templateDriveFilename))
        {
            var exact = files.FirstOrDefault(f =>
                string.Equals(f.Name, templateDriveFilename, StringComparison.OrdinalIgnoreCase));
            if (exact is not null) return exact;

            var stem = StripExtension(templateDriveFilename);
            var byStem = files.FirstOrDefault(f =>
                string.Equals(StripExtension(f.Name), stem, StringComparison.OrdinalIgnoreCase));
            if (byStem is not null) return byStem;
        }

        if (string.IsNullOrWhiteSpace(codigoArtefacto)) return null;

        var pattern = CodigoToRegex(codigoArtefacto);
        if (pattern is null) return null;

        DriveFile? mejor = null;
        var mejorScore = -1;
        foreach (var f in files)
        {
            if (!pattern.IsMatch(f.Name)) continue;

            var score = f.Name.Length;
            if (mejor is null || score < mejorScore)
            {
                mejor = f;
                mejorScore = score;
            }
        }

        return mejor;
    }

    /// <summary>
    /// "FR 30" / "FR-30" / "FR30" → regex que matchea cualquiera de esas
    /// variantes como token dentro del nombre de archivo.
    /// </summary>
    private static Regex? CodigoToRegex(string codigo)
    {
        var trimmed = codigo.Trim();
        if (trimmed.Length == 0) return null;

        var prefixSepNumero = PrefixSepNumeroRegex().Match(trimmed);
        string pattern;
        if (prefixSepNumero.Success)
        {
            var prefix = Regex.Escape(prefixSepNumero.Groups[1].Value);
            var numero = prefixSepNumero.Groups[2].Value;
            pattern = $@"(?<![A-Za-z0-9]){prefix}[\s._\-]*{numero}(?![0-9])";
        }
        else
        {
            pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(trimmed)}(?![A-Za-z0-9])";
        }

        return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            TimeSpan.FromMilliseconds(100));
    }

    private static string StripExtension(string name)
    {
        var dot = name.LastIndexOf('.');
        return dot > 0 ? name[..dot] : name;
    }

    private static bool IsGoogleDriveUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.StartsWith("https://drive.google.com", StringComparison.OrdinalIgnoreCase)
               || url.StartsWith("https://docs.google.com", StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"^([A-Za-z]+)[\s._\-]*([0-9]+)$", RegexOptions.CultureInvariant)]
    private static partial Regex PrefixSepNumeroRegex();
}
