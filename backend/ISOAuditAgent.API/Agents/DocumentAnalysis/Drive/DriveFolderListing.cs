namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Resultado de la tool MCP <c>list_files_under_folder</c>: listado de
/// archivos bajo una carpeta arbitraria de Drive, sin requerir mapeo de
/// proyecto. Usado, por ejemplo, para resolver templates en
/// <c>configuracion_sistema.drive_carpeta_templates_id</c>.
/// </summary>
public sealed record DriveFolderListing(
    string FolderId,
    IReadOnlyList<DriveFile> Files);
