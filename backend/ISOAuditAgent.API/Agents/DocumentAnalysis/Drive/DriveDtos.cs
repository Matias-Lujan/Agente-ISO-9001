// ============================================================================
//  DTOs del subsistema Drive (D3.1) — contrato entre server MCP y cliente MCP
// ----------------------------------------------------------------------------
//  Estos DTOs se serializan a JSON cuando el server MCP devuelve un resultado,
//  y se deserializan del otro lado en el cliente MCP. Por eso son records
//  simples sin lógica.
//
//  ALCANCE D3.1: solo lo que precisa el smoke endpoint (listar archivos +
//  descargar bytes). DriveFolder, DriveItem polimórfico y otras variantes del
//  diff viejo NO entran acá — eran complejidad que el MVP no necesita.
// ============================================================================

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Drive;

/// <summary>
/// Un archivo encontrado en Drive. NombreArchivo + Id + MimeType es lo que el
/// agente necesita para decidir si descargarlo y cómo parsearlo. Path es la
/// ruta relativa respecto al folderId raíz del listado (ej.
/// "Seguimiento/FR 29-05 Tailoring (30.052).xlsx"). WebViewLink se conserva
/// porque el FR-29 puede referenciar archivos por esa URL.
/// </summary>
public sealed record DriveFile(
    string Id,
    string Name,
    string MimeType,
    string? WebViewLink,
    long? Size,
    string Path);

/// <summary>
/// Resultado de listar recursivamente un folder de Drive. Files es una lista
/// plana de archivos (sin carpetas) con path relativo al FolderId raíz.
/// </summary>
public sealed record DriveFolderListing(
    string FolderId,
    IReadOnlyList<DriveFile> Files);

/// <summary>
/// Resultado de descargar un archivo. Bytes en base64 se serializan
/// automáticamente como string en el JSON de la respuesta MCP.
/// ErrorExport != null indica que el archivo existe en Drive pero el export
/// de formato nativo Google falló (ej: supera el límite de 10 MB de la API).
/// En ese caso Bytes está vacío y el artefacto debe marcarse ExportFallido.
/// </summary>
public sealed record DriveFileContent(
    string Id,
    string Name,
    string MimeType,
    byte[] Bytes,
    long? Size,
    string? ErrorExport = null);