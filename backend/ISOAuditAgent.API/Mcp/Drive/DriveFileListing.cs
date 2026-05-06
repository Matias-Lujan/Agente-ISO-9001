using ISOAuditAgent.DocumentAnalysis.Drive;

namespace ISOAuditAgent.API.Mcp.Drive;

/// <summary>
/// Resultado de la herramienta MCP <c>list_project_files</c>: archivos
/// candidatos del proyecto, ya filtrados por MIME types permitidos y con
/// las carpetas excluidas podadas.
/// </summary>
public sealed record DriveFileListing(
    int ProyectoId,
    string FolderId,
    IReadOnlyList<DriveFile> Files
);
