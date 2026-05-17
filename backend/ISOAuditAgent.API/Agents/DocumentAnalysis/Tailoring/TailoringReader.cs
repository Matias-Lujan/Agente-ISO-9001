using System.Text.RegularExpressions;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.DocumentAnalysis.Mcp;
using ISOAuditAgent.DocumentAnalysis.Parsing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace ISOAuditAgent.DocumentAnalysis.Tailoring;

/// <summary>
/// Implementación por defecto de <see cref="ITailoringReader"/>: lista el
/// proyecto en Drive vía MCP, elige un candidato XLSX de tailoring (FR 29) y
/// parsea filas con <see cref="XlsxTableExtractor"/>.
/// </summary>
public sealed class TailoringReader : ITailoringReader
{
    private const string XlsxMime =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly Regex Fr29InName = new(
        @"FR[\s._-]*29",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private readonly IDriveMcpClient _mcp;
    private readonly XlsxTableExtractor _tables;
    private readonly ILogger<TailoringReader> _logger;

    public TailoringReader(
        IDriveMcpClient mcp,
        XlsxTableExtractor tables,
        ILogger<TailoringReader>? logger = null)
    {
        _mcp = mcp ?? throw new ArgumentNullException(nameof(mcp));
        _tables = tables ?? throw new ArgumentNullException(nameof(tables));
        _logger = logger ?? NullLogger<TailoringReader>.Instance;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<EntradaTailoring>> ReadAsync(
        int proyectoId,
        CancellationToken cancellationToken = default)
    {
        var candidates = await CollectTailoringCandidatesAsync(proyectoId, cancellationToken)
            .ConfigureAwait(false);

        if (candidates.Count == 0)
            throw new TailoringWorkbookNotFoundException(proyectoId);

        var chosen = SelectBestCandidate(candidates);
        _logger.LogInformation(
            "Tailoring FR 29: proyecto {ProyectoId} → archivo '{Nombre}' ({FileId}).",
            proyectoId, chosen.Name, chosen.Id);

        var file = await _mcp.GetFileContentAsync(chosen.Id, cancellationToken)
            .ConfigureAwait(false);

        if (!file.MimeType.Equals(XlsxMime, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"El tailoring seleccionado '{file.Name}' no es XLSX (MIME '{file.MimeType}').");
        }

        using var ms = new MemoryStream(file.Bytes, writable: false);

        var (_, headers, dataRows) = _tables.ExtractKeyedTable(
            ms,
            file.Name,
            preferredSheetName: "Tailoring",
            cancellationToken: cancellationToken);

        if (!TailoringColumnMapper.HeadersSuggestCodigoColumn(headers))
        {
            throw new InvalidOperationException(
                $"El workbook '{file.Name}' no tiene una columna de código de artefacto " +
                $"reconocible (esperado algo como «Código», «FR», «Referencia»). " +
                $"Si la planilla del cliente difiere, use la tool del agente LLM (Fase F).");
        }

        var entradas = new List<EntradaTailoring>();
        foreach (var row in dataRows)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var codigo = TailoringColumnMapper.GetCodigo(row);
            if (string.IsNullOrWhiteSpace(codigo))
                continue;

            entradas.Add(new EntradaTailoring(
                CodigoArtefacto: codigo.Trim(),
                EtapaNombre: TailoringColumnMapper.GetEtapa(row),
                Aplica: TailoringColumnMapper.ParseAplica(
                    TailoringColumnMapper.GetAplicaRaw(row)),
                JustificacionNoAplica: TailoringColumnMapper.GetJustificacion(row),
                UrlReferencia: TailoringColumnMapper.GetUrl(row)));
        }

        if (entradas.Count == 0 && dataRows.Count > 0)
        {
            throw new InvalidOperationException(
                $"El workbook '{file.Name}' tiene {dataRows.Count} filas de datos pero " +
                $"ningún código de artefacto en columnas reconocidas.");
        }

        return entradas;
    }

    private async Task<List<DriveFile>> CollectTailoringCandidatesAsync(
        int proyectoId,
        CancellationToken cancellationToken)
    {
        var listing = await _mcp.ListProjectFilesAsync(proyectoId, cancellationToken)
            .ConfigureAwait(false);

        return listing.Files.Where(IsTailoringCandidate).ToList();
    }

    private static bool IsTailoringCandidate(DriveFile file)
    {
        if (!file.MimeType.Equals(XlsxMime, StringComparison.OrdinalIgnoreCase))
            return false;

        return ScoreTailoringName(file.Name) > 0;
    }

    private static int ScoreTailoringName(string fileName)
    {
        var n = fileName.Trim();
        var score = 0;
        if (Fr29InName.IsMatch(n)) score += 10;
        if (n.Contains("tailoring", StringComparison.OrdinalIgnoreCase))
            score += 5;
        return score;
    }

    private static DriveFile SelectBestCandidate(IReadOnlyList<DriveFile> files)
    {
        return files
            .OrderByDescending(f => ScoreTailoringName(f.Name))
            .ThenBy(f => f.Name.Length)
            .ThenBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .First();
    }
}
