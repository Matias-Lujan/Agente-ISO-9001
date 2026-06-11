// ============================================================================
//  TailoringReader — Lectura del FR-29 desde un folder de Drive (D3.4)
// ----------------------------------------------------------------------------
//  Responsabilidad acotada:
//   1. Listar el folder de Drive vía MCP.
//   2. Encontrar el archivo candidato más probable a ser FR-29.
//   3. Descargarlo vía MCP.
//   4. Parsear con ClosedXML.
//   5. Mapear filas a FilaTailoring.
// ============================================================================

using ClosedXML.Excel;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using System.Text.RegularExpressions;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;

public sealed class TailoringReader
{
    private const string XlsxMime =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private const string GoogleSheetsMime =
        "application/vnd.google-apps.spreadsheet";

    private static readonly Regex Fr29EnNombre = new(
        @"FR[\s._-]*29",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(200));

    private const int MaxFilasParaBuscarHeader = 15;

    private readonly IDriveMcpClient _drive;
    private readonly ILogger<TailoringReader> _logger;

    public TailoringReader(
        IDriveMcpClient drive,
        ILogger<TailoringReader> logger)
    {
        _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyList<FilaTailoring>> LeerAsync(
        string driveFolderId, CancellationToken ct)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(driveFolderId);

        var listing = await _drive
            .ListFilesUnderFolderAsync(driveFolderId, ct)
            .ConfigureAwait(false);

        var candidatos = listing.Files
            .Where(EsCandidatoFr29)
            .OrderByDescending(f => ScoreFr29(f.Name))
            .ThenBy(f => f.Name.Length)
            .ToList();

        if (candidatos.Count == 0)
        {
            throw new InvalidOperationException(
                $"No se encontró un archivo FR-29 (tailoring XLSX) bajo el " +
                $"folder de Drive '{driveFolderId}'.");
        }

        var elegido = candidatos[0];
        _logger.LogInformation(
            "TailoringReader: folder {FolderId} → FR-29 elegido '{Nombre}' ({FileId}).",
            driveFolderId, elegido.Name, elegido.Id);

        var content = await _drive
            .GetFileContentAsync(elegido.Id, ct)
            .ConfigureAwait(false);

        if (content.ErrorExport is not null)
        {
            throw new InvalidOperationException(
                $"El archivo FR-29 '{elegido.Name}' fue encontrado en Drive pero no pudo exportarse. " +
                $"Detalle: {content.ErrorExport}. " +
                $"Convertí el archivo a formato .xlsx estándar en el Drive del proyecto.");
        }

        if (!string.Equals(content.MimeType, XlsxMime, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"El archivo elegido como FR-29 ('{content.Name}') no es XLSX " +
                $"(MIME real: '{content.MimeType}').");
        }

        _logger.LogInformation(
            "TailoringReader: descargado {Nombre} ({Bytes} bytes). Iniciando ParsearWorkbook.",
            content.Name, content.Bytes.Length);
        var filas = ParsearWorkbook(content.Bytes, content.Name);
        _logger.LogInformation(
            "TailoringReader: ParsearWorkbook completado — {N} filas de tailoring.",
            filas.Count);
        return filas;
    }

    private static bool EsCandidatoFr29(DriveFile f)
    {
        bool esXlsx = string.Equals(f.MimeType, XlsxMime, StringComparison.OrdinalIgnoreCase);
        bool esGoogleSheets = string.Equals(f.MimeType, GoogleSheetsMime, StringComparison.OrdinalIgnoreCase);

        if (!esXlsx && !esGoogleSheets)
            return false;

        return ScoreFr29(f.Name) > 0;
    }

    private static int ScoreFr29(string nombre)
    {
        var n = nombre?.Trim() ?? string.Empty;
        var score = 0;

        if (Fr29EnNombre.IsMatch(n)) score += 10;
        if (n.Contains("tailoring", StringComparison.OrdinalIgnoreCase)) score += 5;

        return score;
    }

    private static IReadOnlyList<FilaTailoring> ParsearWorkbook(byte[] bytes, string nombreArchivo)
    {
        try
        {
            using var ms = new MemoryStream(bytes, writable: false);
            using var workbook = new XLWorkbook(ms);

            var hoja = workbook.Worksheets
                .FirstOrDefault(s => string.Equals(
                    s.Name, "Tailoring", StringComparison.OrdinalIgnoreCase))
                ?? workbook.Worksheets.FirstOrDefault()
                ?? throw new InvalidOperationException(
                    $"El workbook '{nombreArchivo}' no tiene hojas.");

            var rango = hoja.RangeUsed();
            if (rango is null)
                return Array.Empty<FilaTailoring>();

            var (headerRowNumber, columnMap) =
                DetectarHeaderConArtefactoYAplica(rango, nombreArchivo);

            return LeerFilasDeDatos(hoja, headerRowNumber, columnMap);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Error parseando FR-29 '{nombreArchivo}': {ex.Message}", ex);
        }
    }

    private static (int RowNumber, ColumnMap Map) DetectarHeaderConArtefactoYAplica(
        IXLRange rango, string nombreArchivo)
    {
        var filasCandidatas = rango.RowsUsed()
            .Take(MaxFilasParaBuscarHeader)
            .ToList();

        var headersDescartados = new List<(int Row, string Joined)>();

        foreach (var fila in filasCandidatas)
        {
            int? artefactoCol = null;
            int? aplicaCol = null;
            int? codigoCol = null;
            int? justificacionUrlCol = null;

            var headersDeFila = new List<string>();

            foreach (var celda in fila.Cells())
            {
                var raw = celda.GetString();
                var norm = TailoringColumnMapper.NormalizeHeaderKey(raw);
                headersDeFila.Add(norm);

                if (string.IsNullOrEmpty(norm))
                    continue;

                var col = celda.Address.ColumnNumber;

                if (artefactoCol is null && EsHeaderArtefacto(norm))
                    artefactoCol = col;
                else if (aplicaCol is null && EsHeaderAplica(norm))
                    aplicaCol = col;
                else if (codigoCol is null && EsHeaderCodigo(norm))
                    codigoCol = col;
                else if (justificacionUrlCol is null && EsHeaderJustificacionUrl(norm))
                    justificacionUrlCol = col;
            }

            if (artefactoCol is not null && aplicaCol is not null)
            {
                return (fila.RowNumber(), new ColumnMap(
                    Artefacto: artefactoCol.Value,
                    Aplica: aplicaCol.Value,
                    Codigo: codigoCol,
                    JustificacionUrl: justificacionUrlCol));
            }

            headersDescartados.Add((fila.RowNumber(), string.Join(", ", headersDeFila)));
        }

        var detalle = string.Join("; ", headersDescartados
            .Select(h => $"fila {h.Row}: [{h.Joined}]"));

        throw new InvalidOperationException(
            $"El workbook '{nombreArchivo}' no tiene una fila de encabezados con " +
            $"AL MENOS las columnas 'Artefacto' y 'Aplica' en las primeras " +
            $"{MaxFilasParaBuscarHeader} filas usadas. Headers descartados: {detalle}.");
    }

    private static IReadOnlyList<FilaTailoring> LeerFilasDeDatos(
        IXLWorksheet hoja, int headerRowNumber, ColumnMap map)
    {
        var ultimaFila = hoja.LastRowUsed()?.RowNumber() ?? headerRowNumber;
        var filas = new List<FilaTailoring>();

        for (int r = headerRowNumber + 1; r <= ultimaFila; r++)
        {
            var nombre = hoja.Cell(r, map.Artefacto).GetString().Trim();
            if (string.IsNullOrWhiteSpace(nombre))
                continue;

            var aplicaRaw = hoja.Cell(r, map.Aplica).GetString();
            var aplica = TailoringColumnMapper.ParseAplica(aplicaRaw);

            string? codigo = null;
            if (map.Codigo is int codigoCol)
            {
                var c = hoja.Cell(r, codigoCol).GetString().Trim();
                if (!string.IsNullOrEmpty(c))
                    codigo = c;
            }

            string? justificacion = null;
            string? url = null;
            if (map.JustificacionUrl is int justificacionUrlCol)
            {
                var celda = hoja.Cell(r, justificacionUrlCol).GetString().Trim();
                if (!string.IsNullOrEmpty(celda))
                    (justificacion, url) = HeuristicaJustificacionOUrl(celda);
            }

            filas.Add(new FilaTailoring(
                CodigoArtefacto: codigo,
                NombreArtefacto: nombre,
                Aplica: aplica,
                JustificacionNoAplica: justificacion,
                UrlReferencia: url));
        }

        return filas;
    }

    private static bool EsHeaderArtefacto(string norm) =>
        norm == "artefacto"
        || norm.Contains("nombre artefacto", StringComparison.Ordinal)
        || norm.Contains("documento", StringComparison.Ordinal);

    private static bool EsHeaderAplica(string norm) =>
        norm == "aplica"
        || norm.Contains("aplica en proyecto", StringComparison.Ordinal);

    private static bool EsHeaderCodigo(string norm) =>
        norm.Contains("codigo documento formal", StringComparison.Ordinal)
        || norm.Contains("codigo artefacto", StringComparison.Ordinal)
        || norm == "codigo"
        || norm == "referencia";

    private static bool EsHeaderJustificacionUrl(string norm) =>
        norm.Contains("justificacion o ubicacion", StringComparison.Ordinal)
        || norm.Contains("justificacion", StringComparison.Ordinal)
        || norm.Contains("ubicacion", StringComparison.Ordinal)
        || norm.Contains("url", StringComparison.Ordinal)
        || norm.Contains("link", StringComparison.Ordinal)
        || norm.Contains("enlace", StringComparison.Ordinal);

    private static (string? Justificacion, string? Url) HeuristicaJustificacionOUrl(string celda)
    {
        if (celda.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || celda.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || celda.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
        {
            return (null, celda);
        }

        return (celda, null);
    }

    private sealed record ColumnMap(
        int Artefacto,
        int Aplica,
        int? Codigo,
        int? JustificacionUrl);
}
