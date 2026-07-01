namespace ISOAuditAgent.API.Agents.Shared;

/// <summary>
/// No se pudo encontrar en la carpeta de Drive del proyecto un documento que la
/// auditoría necesitaba (por ejemplo, la planilla de tailoring FR-29). Es un
/// caso distinto de un fallo de conexión/credenciales de Drive: el Drive
/// respondió, pero el archivo no está (o su nombre no permite identificarlo).
///
/// El clasificador la mapea a <c>CategoriaErrorAuditoria.DocumentoNoEncontrado</c>
/// y el mensaje ya viene redactado para mostrárselo al auditor.
/// </summary>
public sealed class DocumentoDriveNoEncontradoException : Exception
{
    /// <summary>Descripción del documento buscado (ej. "planilla de tailoring FR-29").</summary>
    public string Documento { get; }

    public DocumentoDriveNoEncontradoException(string documento, string mensaje)
        : base(mensaje)
    {
        Documento = documento;
    }
}
