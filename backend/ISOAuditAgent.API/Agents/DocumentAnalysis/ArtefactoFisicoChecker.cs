// ============================================================================
//  ArtefactoFisicoChecker — Implementación de IArtefactoFisicoChecker (D3.5)
// ----------------------------------------------------------------------------
//  Cuatro pasos de búsqueda del documento del proyecto, en orden de
//  confiabilidad:
//
//   PASO 1 — Por URL del tailoring (FR-29). Si urlReferenciaTailoring contiene
//            un fileId extraíble, se descarga ese archivo. Si la URL pega un
//            archivo válido, ese ES el artefacto — sin verificación adicional
//            (la URL es palabra del usuario).
//
//   PASO 2 — Por código del artefacto. Normaliza el código (lowercase + sin
//            espacios/guiones/underscore) y busca match en el nombre normalizado
//            de algún archivo del folder.
//            Si el artefacto TIENE código formal y ningún archivo lo contiene →
//            retorna NoEncontrado. No cae a PASO 3/4: un archivo sin el código
//            en su nombre no puede ser el artefacto correcto (evita que "FR 25"
//            matchee "PR 11-13 Liberación de Software").
//
//   PASO 3 — Por nombre del artefacto. SOLO corre si el artefacto NO tiene
//            código formal (codigoArtefacto es null o vacío). Normaliza nombre
//            y nombres de archivo (lowercase + sin tildes + colapso de
//            whitespace) y busca match por subcadena.
//
//   PASO 4 — Por segmentos del path de carpeta. SOLO corre si el artefacto NO
//            tiene código formal y PASO 1-3 fallaron. Extrae tokens
//            significativos del nombre (≥4 chars, sin stopwords) y los busca
//            en los segmentos del Path relativo de cada archivo. Resuelve casos
//            como "Cronograma del proyecto" → archivo en Seguimiento/Cronograma/
//            (ej. "30.052 - NTV - App_v1.mpp").
//
//  Después de encontrar el documento del proyecto:
//    - SHA-256 con ContentHasher.Sha256Hex.
//    - Parser según MIME (DOCX/XLSX/PDF). Otro MIME → Secciones = [].
//    - Excepción del parser → Secciones = [] (no falla la verificación; el
//      archivo SÍ está, las secciones son del paso siguiente del pipeline).
//
//  Template: si nombreTemplateArchivo != null y integraciones.TemplatesFolderId
//    != null, se busca el template en la carpeta de templates de Drive, se parsea
//    y se devuelve en SeccionesTemplate. Errores de template no abortan la
//    verificación del documento — SeccionesTemplate queda [] en ese caso.
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
        Secciones: Array.Empty<SeccionDetectada>(),
        SeccionesTemplate: Array.Empty<SeccionDetectada>(),
        NombreTemplateArchivo: null);

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
        string? nombreTemplateArchivo,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(integraciones);
        ArgumentException.ThrowIfNullOrWhiteSpace(nombreArtefacto);

        _logger.LogInformation("VerificarAsync: artefacto {Id} ({Nombre}) template={Template}.", artefactoEsperadoId, nombreArtefacto, nombreTemplateArchivo ?? "(sin template)");
        var driveFolderId = integraciones.DriveFolderId
            ?? throw new InvalidOperationException(
                "ArtefactoFisicoChecker requiere Integraciones.DriveFolderId. " +
                "ArtefactosBuilder debería garantizarlo antes de invocar.");

        // Helper local: a partir del documento encontrado, busca y parsea el
        // template, luego ensambla el VerificacionFisica final.
        async ValueTask<VerificacionFisica> ArmarResultadoAsync(DriveFileContent doc)
        {
            var (seccionesTemplate, nombreTemplateEncontrado) =
                await FetchTemplateAsync(
                    integraciones.TemplatesFolderId,
                    nombreTemplateArchivo,
                    ct);

            return ArmarVerificacionEncontrado(doc, seccionesTemplate, nombreTemplateEncontrado);
        }

        // --- PASO 1: URL del tailoring ---
        var fileIdDesdeUrl = DriveUrlHelper.TryExtractFileId(urlReferenciaTailoring);
        if (fileIdDesdeUrl is not null)
        {
            try
            {
                var content = await _drive.GetFileContentAsync(fileIdDesdeUrl, ct);
                if (content.ErrorExport is not null)
                    return VerificacionExportFallida(content, artefactoEsperadoId);
                _logger.LogInformation(
                    "Artefacto {Id}: encontrado por URL del tailoring → '{Nombre}'.",
                    artefactoEsperadoId, content.Name);
                return await ArmarResultadoAsync(content);
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
                    if (content.ErrorExport is not null)
                        return VerificacionExportFallida(content, artefactoEsperadoId);
                    _logger.LogInformation(
                        "Artefacto {Id}: encontrado por código '{Codigo}' → '{Nombre}'.",
                        artefactoEsperadoId, codigoArtefacto, content.Name);
                    return await ArmarResultadoAsync(content);
                }
            }

            // Código formal presente pero sin archivo matching: no caer a nombre/path.
            // Un archivo sin el código en su nombre no es evidencia válida de este
            // artefacto (evita que "FR 25" matchee "PR 11-13 Liberación de Software").
            _logger.LogInformation(
                "Artefacto {Id} (codigo='{Codigo}', nombre='{Nombre}'): " +
                "no encontrado por código; fallback por nombre omitido " +
                "(artefacto tiene código formal).",
                artefactoEsperadoId, codigoArtefacto, nombreArtefacto);
            return NoEncontrado;
        }

        // --- PASO 3: nombre del artefacto (solo si no tiene código formal) ---
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
                if (content.ErrorExport is not null)
                    return VerificacionExportFallida(content, artefactoEsperadoId);
                _logger.LogInformation(
                    "Artefacto {Id}: encontrado por nombre '{Nombre}' → '{Archivo}'.",
                    artefactoEsperadoId, nombreArtefacto, content.Name);
                return await ArmarResultadoAsync(content);
            }
        }

        // --- PASO 4: por path de carpeta (solo si no tiene código formal).
        //     Solo corre si PASO 1-3 fallaron, así no toca lo que ya resuelve. ---
        var tokensPath = ExtraerTokensPath(nombreArtefacto);
        if (tokensPath.Count > 0)
        {
            var match = listing.Files
                .Where(f => !string.IsNullOrEmpty(f.Path) && f.Path
                    .Split('/', StringSplitOptions.RemoveEmptyEntries)
                    .Any(seg =>
                    {
                        var segNorm = TailoringColumnMapper.NormalizeHeaderKey(seg);
                        return tokensPath.Any(t => segNorm.Contains(t, StringComparison.Ordinal));
                    }))
                .OrderBy(f => f.Name, StringComparer.Ordinal)
                .LastOrDefault(); // presencia: cualquiera sirve; ordeno por reproducibilidad

            if (match is not null)
            {
                var content = await _drive.GetFileContentAsync(match.Id, ct);
                if (content.ErrorExport is not null)
                    return VerificacionExportFallida(content, artefactoEsperadoId);
                _logger.LogInformation(
                    "Artefacto {Id}: encontrado por path de carpeta '{Path}' → '{Archivo}'.",
                    artefactoEsperadoId, match.Path, content.Name);
                return await ArmarResultadoAsync(content);
            }
        }

        _logger.LogInformation(
            "Artefacto {Id} (codigo='{Codigo}', nombre='{Nombre}'): no encontrado en folder {Folder}.",
            artefactoEsperadoId, codigoArtefacto, nombreArtefacto, driveFolderId);

        return NoEncontrado;
    }

    // -------------------------------------------------------------------------
    //  Template fetching: busca el archivo de template en la carpeta de
    //  templates de Drive y parsea sus secciones. Robusto: cualquier error
    //  devuelve lista vacía sin abortar la verificación del documento.
    // -------------------------------------------------------------------------

    private async ValueTask<(IReadOnlyList<SeccionDetectada> Secciones, string? NombreArchivo)>
        FetchTemplateAsync(
            string? templatesFolderId,
            string? nombreTemplateArchivo,
            CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(templatesFolderId)
            || string.IsNullOrWhiteSpace(nombreTemplateArchivo))
        {
            return (Array.Empty<SeccionDetectada>(), null);
        }

        _logger.LogInformation("FetchTemplate: iniciando para {Template} en folder {Folder}.", nombreTemplateArchivo, templatesFolderId);
        try
        {
            _logger.LogInformation(
                "FetchTemplate: llamando ListFilesUnderFolder en {Folder}.", templatesFolderId);
            var listing = await _drive.ListFilesUnderFolderAsync(templatesFolderId, ct);
            _logger.LogInformation("FetchTemplate: listing OK ({N} archivos).", listing.Files.Count);
            var match = listing.Files.FirstOrDefault(f =>
                string.Equals(f.Name, nombreTemplateArchivo, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                _logger.LogWarning(
                    "Template '{Nombre}' no encontrado en carpeta {FolderId}. " +
                    "Continúo sin secciones de template.",
                    nombreTemplateArchivo, templatesFolderId);
                return (Array.Empty<SeccionDetectada>(), null);
            }

            var content = await _drive.GetFileContentAsync(match.Id, ct);
            var secciones = ParsearSeccionesSeguro(content, CrearParser(content.MimeType));

            _logger.LogInformation(
                "Template '{Nombre}' obtenido: {Count} secciones.",
                content.Name, secciones.Count);

            return (secciones, content.Name);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error al obtener template '{Nombre}' de carpeta {FolderId}. " +
                "Continúo sin secciones de template.",
                nombreTemplateArchivo, templatesFolderId);
            return (Array.Empty<SeccionDetectada>(), null);
        }
    }

    private VerificacionFisica ArmarVerificacionEncontrado(
        DriveFileContent content,
        IReadOnlyList<SeccionDetectada> seccionesTemplate,
        string? nombreTemplateArchivo)
    {
        var hash = ContentHasher.Sha256Hex(content.Bytes);
        var parser = CrearParser(content.MimeType);
        var secciones = ParsearSeccionesSeguro(content, parser);
        var (vigencia, codigo, proyecto, parseable) = ExtraerMetadataSegura(content, parser);

        return new VerificacionFisica(
            Encontrado: true,
            Fuente: FuenteDocumento.Drive,
            NombreArchivo: content.Name,
            HashContenido: hash,
            Secciones: secciones,
            SeccionesTemplate: seccionesTemplate,
            NombreTemplateArchivo: nombreTemplateArchivo,
            VigenciaDetectada: vigencia,
            DocumentoParseable: parseable,
            CodigoDetectado: codigo,
            ProyectoDetectado: proyecto);
    }

    /// <summary>
    /// Intenta parsear secciones según el MIME. Si el MIME no es soportado o
    /// el parser tira, devuelve lista vacía SIN propagar — el archivo SÍ
    /// existe; las secciones son input para ConsistencyVerification que puede
    /// tolerar ausencia.
    /// </summary>
    /// <summary>
    /// Crea el parser según el MIME, o null si el formato no es soportado.
    /// </summary>
    private static ITemplateParser? CrearParser(string mimeType) => mimeType switch
    {
        MimeDocx => new DocxTemplateParser(),
        MimeXlsx => new XlsxTemplateParser(),
        MimePdf => new PdfTemplateParser(),
        _ => null
    };

    private IReadOnlyList<SeccionDetectada> ParsearSeccionesSeguro(
        DriveFileContent content, ITemplateParser? parser)
    {
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
    /// Extrae la vigencia del documento y reporta si el formato es parseable.
    /// - parser null (formato no soportado) → (null, false) + log/warning: no se
    ///   valida vigencia (regla acordada: "formato no parseable → solo log").
    /// - parser presente → (vigencia detectada o null, true). Que sea parseable y
    ///   no tenga vigencia detectada lo resuelve aguas abajo como OBS.
    /// </summary>
    private (DateOnly? Vigencia, string? Codigo, string? Proyecto, bool Parseable) ExtraerMetadataSegura(
        DriveFileContent content, ITemplateParser? parser)
    {
        if (parser is null)
        {
            _logger.LogWarning(
                "Documento '{Archivo}': formato '{Mime}' no soportado; no se valida " +
                "vigencia/código/proyecto del formulario.",
                content.Name, content.MimeType);
            return (null, null, null, false);
        }

        try
        {
            var bloques = parser.ExtraerBloquesMetadata(content.Bytes);
            var vigencia = MetadataScanner.DetectarVigencia(bloques);
            var codigo = MetadataScanner.DetectarCodigo(bloques);
            var proyecto = MetadataScanner.DetectarProyecto(bloques);

            if (vigencia is null)
            {
                // Diagnóstico: dejamos en el log el texto que efectivamente se
                // escaneó, para poder ver SIN adivinar por qué no se detectó la
                // vigencia (¿falta el label? ¿la fecha está en otro formato/lugar?).
                var muestra = string.Join(" ⏐ ", bloques).ReplaceLineEndings(" ");
                if (muestra.Length > 800) muestra = muestra[..800] + "…";
                _logger.LogWarning(
                    "No se detectó la vigencia en '{Archivo}' ({Mime}). " +
                    "Texto escaneado (recortado): «{Texto}»",
                    content.Name, content.MimeType, muestra);
            }

            _logger.LogInformation(
                "Metadata de '{Archivo}': vigencia={Vig}, código={Cod}, proyecto={Proy}.",
                content.Name,
                vigencia?.ToString("dd/MM/yyyy") ?? "(no detectada)",
                codigo ?? "(no detectado)",
                proyecto ?? "(no detectado)");

            return (vigencia, codigo, proyecto, true);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Error extrayendo metadata de '{Archivo}' ({Mime}); continúo sin metadata.",
                content.Name, content.MimeType);
            return (null, null, null, true); // formato soportado pero falló → parseable, sin metadata
        }
    }

    private static readonly HashSet<string> StopwordsPath = new(StringComparer.Ordinal)
    {
        "de", "del", "la", "el", "los", "las", "y", "en",
        "proyecto", "documento", "documentos", "registro", "registros"
    };

    /// <summary>
    /// Tokens significativos del nombre del artefacto para matchear contra
    /// segmentos de path. Normaliza con la misma rutina del PASO 3, descarta
    /// stopwords/genéricas y exige longitud ≥ 4. "Cronograma del proyecto" → ["cronograma"].
    /// </summary>
    private static IReadOnlyList<string> ExtraerTokensPath(string nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre)) return Array.Empty<string>();

        return nombre
            .Split(new[] { ' ', '\t', '-', '_', '/' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(TailoringColumnMapper.NormalizeHeaderKey)
            .Where(t => t.Length >= 4 && !StopwordsPath.Contains(t))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    /// <summary>
    /// Devuelve una VerificacionFisica con ExportFallido=true cuando el archivo
    /// existe en Drive pero su export falló (formato nativo Google, ej: >10 MB).
    /// No es NoEncontrado: el archivo existe, pero no es procesable sin su contenido.
    /// El hallazgo determinístico correspondiente se genera en HallazgosDeterministicos.
    /// </summary>
    private VerificacionFisica VerificacionExportFallida(
        DriveFileContent content, int artefactoEsperadoId)
    {
        _logger.LogWarning(
            "Artefacto {Id}: archivo '{Nombre}' encontrado en Drive pero export fallido. " +
            "Se marca ExportFallido — verificación manual requerida. Detalle: {Error}",
            artefactoEsperadoId, content.Name, content.ErrorExport);

        return new VerificacionFisica(
            Encontrado: false,
            Fuente: FuenteDocumento.Drive,
            NombreArchivo: content.Name,
            HashContenido: null,
            Secciones: Array.Empty<SeccionDetectada>(),
            SeccionesTemplate: Array.Empty<SeccionDetectada>(),
            NombreTemplateArchivo: null,
            ExportFallido: true);
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
