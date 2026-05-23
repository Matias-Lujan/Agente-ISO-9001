// ============================================================================
//  ArtefactoFisicoChecker — Implementación de IArtefactoFisicoChecker (D3.5)
// ----------------------------------------------------------------------------
//  Tres pasos de búsqueda, en orden de confiabilidad:
//
//   PASO 1 — Por URL del tailoring (FR-29). Si urlReferenciaTailoring contiene
//            un fileId extraíble, se descarga ese archivo. Si la URL pega un
//            archivo válido, ese ES el artefacto — sin verificación adicional
//            (la URL es palabra del usuario).
//
//   PASO 2 — Por código del artefacto. Normaliza el código (lowercase + sin
//            espacios/guiones/underscore) y busca match en el nombre normalizado
//            de algún archivo del folder.
//
//   PASO 3 — Por nombre del artefacto. Normaliza nombre y nombres de archivo
//            (lowercase + sin tildes + colapso de whitespace) y busca match
//            por subcadena.
//
//  Después de encontrar el archivo:
//    - SHA-256 con ContentHasher.Sha256Hex.
//    - Parser según MIME (DOCX/XLSX/PDF). Otro MIME → Secciones = [].
//    - Excepción del parser → Secciones = [] (no falla la verificación; el
//      archivo SÍ está, las secciones son del paso siguiente del pipeline).
//
//  pathTemplateAbsoluto IGNORADO en MVP. El parámetro existe en el contrato
//  para futuras iteraciones con templates locales.
//
//  Caché entre llamadas: NO. Listing del folder se hace en cada VerificarAsync
//  donde haga falta. Para 20-30 artefactos por auditoría son milisegundos
//  contra MCP loopback. Se agrega caché si aparece el problema real.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;
using ISOAuditAgent.API.Agents.DocumentAnalysis.Tailoring;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

public sealed class ArtefactoFisicoChecker : IArtefactoFisicoChecker
{
    private const string MimeDocx =
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
    private const string MimeXlsx =
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string MimePdf = "application/pdf";

    private static readonly VerificacionFisica NoEncontrado = new(
        Encontrado: false,
        Fuente: FuenteDocumento.Drive,
        NombreArchivo: null,
        HashContenido: null,
        Secciones: Array.Empty<SeccionDetectada>());

    private readonly IDriveMcpClient _drive;
    private readonly ILogger<ArtefactoFisicoChecker> _logger;

    public ArtefactoFisicoChecker(
        IDriveMcpClient drive,
        ILogger<ArtefactoFisicoChecker> logger)
    {
        _drive = drive ?? throw new ArgumentNullException(nameof(drive));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async ValueTask<VerificacionFisica> VerificarAsync(
        IntegracionesProyecto integraciones,
        int artefactoEsperadoId,
        string? codigoArtefacto,
        string nombreArtefacto,
        string? urlReferenciaTailoring,
        string? pathTemplateAbsoluto, // ignorado en MVP
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integraciones);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreArtefacto);

        var driveFolderId = integraciones.DriveFolderId
            ?? throw new InvalidOperationException(
                "ArtefactoFisicoChecker requiere Integraciones.DriveFolderId. " +
                "ArtefactosBuilder debería garantizarlo antes de invocar.");

        // --- PASO 1: URL del tailoring ---
        var fileIdDesdeUrl = DriveUrlHelper.TryExtractFileId(urlReferenciaTailoring);
        if (fileIdDesdeUrl is not null)
        {
            try
            {
                var content = await _drive.GetFileContentAsync(fileIdDesdeUrl, ct);
                _logger.LogInformation(
                    "Artefacto {Id}: encontrado por URL del tailoring → '{Nombre}'.",
                    artefactoEsperadoId, content.Name);
                return ArmarVerificacionEncontrado(content);
            }
            catch (Exception ex)
            {
                // URL mal pegada, fileId inexistente, permisos. No abortamos:
                // caemos a búsqueda por código/nombre.
                _logger.LogWarning(ex,
                    "Artefacto {Id}: URL del tailoring presente pero falló la descarga " +
                    "(fileId={FileId}). Sigo con búsqueda por código/nombre.",
                    artefactoEsperadoId, fileIdDesdeUrl);
            }
        }

        // --- Listing del folder para PASO 2 y 3 ---
        var listing = await _drive.ListFilesUnderFolderAsync(driveFolderId, ct);

        // --- PASO 2: código del artefacto ---
        if (!string.IsNullOrWhiteSpace(codigoArtefacto))
        {
            var codigoNorm = NormalizarCodigo(codigoArtefacto);
            if (codigoNorm.Length > 0)
            {
                var match = listing.Files.FirstOrDefault(f =>
                    NormalizarCodigo(f.Name).Contains(codigoNorm, StringComparison.Ordinal));

                if (match is not null)
                {
                    var content = await _drive.GetFileContentAsync(match.Id, ct);
                    _logger.LogInformation(
                        "Artefacto {Id}: encontrado por código '{Codigo}' → '{Nombre}'.",
                        artefactoEsperadoId, codigoArtefacto, content.Name);
                    return ArmarVerificacionEncontrado(content);
                }
            }
        }

        // --- PASO 3: nombre del artefacto ---
        var nombreNorm = TailoringColumnMapper.NormalizeHeaderKey(nombreArtefacto);
        if (nombreNorm.Length > 0)
        {
            var match = listing.Files.FirstOrDefault(f =>
            {
                var fileNorm = TailoringColumnMapper.NormalizeHeaderKey(f.Name);
                return fileNorm.Contains(nombreNorm, StringComparison.Ordinal)
                    || nombreNorm.Contains(fileNorm, StringComparison.Ordinal);
            });

            if (match is not null)
            {
                var content = await _drive.GetFileContentAsync(match.Id, ct);
                _logger.LogInformation(
                    "Artefacto {Id}: encontrado por nombre '{Nombre}' → '{Archivo}'.",
                    artefactoEsperadoId, nombreArtefacto, content.Name);
                return ArmarVerificacionEncontrado(content);
            }
        }

        _logger.LogInformation(
            "Artefacto {Id} (codigo='{Codigo}', nombre='{Nombre}'): no encontrado en folder {Folder}.",
            artefactoEsperadoId, codigoArtefacto, nombreArtefacto, driveFolderId);

        return NoEncontrado;
    }

    private VerificacionFisica ArmarVerificacionEncontrado(DriveFileContent content)
    {
        var hash = ContentHasher.Sha256Hex(content.Bytes);
        var secciones = ParsearSeccionesSeguro(content);

        return new VerificacionFisica(
            Encontrado: true,
            Fuente: FuenteDocumento.Drive,
            NombreArchivo: content.Name,
            HashContenido: hash,
            Secciones: secciones);
    }

    /// <summary>
    /// Intenta parsear secciones según el MIME. Si el MIME no es soportado o
    /// el parser tira, devuelve lista vacía SIN propagar — el archivo SÍ
    /// existe; las secciones son input para ConsistencyVerification que puede
    /// tolerar ausencia.
    /// </summary>
    private IReadOnlyList<SeccionDetectada> ParsearSeccionesSeguro(DriveFileContent content)
    {
        ITemplateParser? parser = content.MimeType switch
        {
            MimeDocx => new DocxTemplateParser(),
            MimeXlsx => new XlsxTemplateParser(),
            MimePdf => new PdfTemplateParser(),
            _ => null
        };

        if (parser is null)
        {
            _logger.LogDebug(
                "MIME '{Mime}' sin parser. Devuelvo Secciones = [].", content.MimeType);
            return Array.Empty<SeccionDetectada>();
        }

        try
        {
            return parser.Parsear(content.Bytes);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Parser de '{Mime}' tiró sobre '{Archivo}'. Continúo con Secciones = []. " +
                "La verificación física sigue siendo Encontrado=true.",
                content.MimeType, content.Name);
            return Array.Empty<SeccionDetectada>();
        }
    }

    /// <summary>
    /// Normaliza un código de artefacto para comparación tolerante:
    /// lowercase + remueve espacios, guiones, underscore y puntos.
    /// "FR 30" / "FR-30" / "FR_30" / "FR30" / "fr.30" → "fr30".
    /// </summary>
    private static string NormalizarCodigo(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return string.Empty;

        var sb = new System.Text.StringBuilder(raw.Length);
        foreach (var c in raw)
        {
            if (char.IsWhiteSpace(c)) continue;
            if (c == '-' || c == '_' || c == '.') continue;
            sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }
}