namespace ISOAuditAgent.DocumentAnalysis.Drive;

/// <summary>
/// Se lanza cuando <see cref="IProyectoDriveResolver.ResolveFolderId(int)"/>
/// no encuentra un mapeo configurado para el <c>ProyectoId</c> solicitado.
/// </summary>
public sealed class ProyectoDriveMappingNotFoundException : InvalidOperationException
{
    /// <summary>
    /// <c>ProyectoId</c> que no pudo resolverse.
    /// </summary>
    public int ProyectoId { get; }

    public ProyectoDriveMappingNotFoundException(int proyectoId)
        : base($"No existe un mapeo Drive configurado para ProyectoId={proyectoId}. " +
               "Verifique la sección 'DocumentAnalysis:Drive:Mappings' en appsettings.")
    {
        ProyectoId = proyectoId;
    }
}
