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
/// agente necesita para decidir si descargarlo y cómo parsearlo. WebViewLink
/// se conserva porque el FR-29 puede referenciar archivos por esa URL.
/// </summary>
public sealed record DriveFile(
    string Id,
    string Name,
    string MimeType,
    string? WebViewLink,
    long? Size);

/// <summary>
/// Resultado de listar un folder de Drive. El FolderId se devuelve para que el
/// caller pueda chequear que el server haya listado lo que pidió.
/// </summary>
public sealed record DriveFolderListing(
    string FolderId,
    IReadOnlyList<DriveFile> Files);

/// <summary>
/// Resultado de descargar un archivo. Bytes en base64 se serializan
/// automáticamente como string en el JSON de la respuesta MCP.
/// </summary>
public sealed record DriveFileContent(
    string Id,
    string Name,
    string MimeType,
    byte[] Bytes,
    long? Size);