namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Resultado de la tool MCP <c>list_project_files</c> (también usado al
/// deserializar la respuesta del cliente MCP).
/// </summary>
public sealed record DriveFileListing(
    int ProyectoId,
    string FolderId,
    IReadOnlyList<DriveFile> Files);
