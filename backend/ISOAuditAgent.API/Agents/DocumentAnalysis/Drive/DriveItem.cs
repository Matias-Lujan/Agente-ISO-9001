namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Elemento devuelto por el listado plano de hijos directos de una carpeta
/// de Google Drive. Discriminada con <see cref="DriveFolder"/> /
/// <see cref="DriveFile"/>.
/// </summary>
public abstract record DriveItem(string Id, string Name);

/// <summary>
/// Subcarpeta dentro de Google Drive.
/// </summary>
public sealed record DriveFolder(string Id, string Name) : DriveItem(Id, Name);

/// <summary>
/// Archivo binario en Google Drive (PDF, DOCX, XLSX, etc.) listado pero
/// aún no descargado.
/// </summary>
public sealed record DriveFile(
    string Id,
    string Name,
    string MimeType,
    string? WebViewLink,
    long? Size,
    DateTimeOffset? CreatedTime,
    DateTimeOffset? ModifiedTime
) : DriveItem(Id, Name);

/// <summary>
/// Bytes y metadatos de un archivo descargado de Google Drive.
/// </summary>
/// <remarks>
/// El consumidor (parsers de DocumentAnalysis, en Fase 4) interpreta
/// <see cref="Bytes"/> según <see cref="MimeType"/>. <see cref="Bytes"/>
/// se serializa como base64 al transportarse vía MCP/JSON-RPC.
/// </remarks>
public sealed record DriveFileContent(
    string Id,
    string Name,
    string MimeType,
    byte[] Bytes,
    long? Size,
    DateTimeOffset? ModifiedTime
);
