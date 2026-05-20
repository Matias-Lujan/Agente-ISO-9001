using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Runner;

/// <summary>
/// Política única para calcular el hash de un documento recolectado según
/// la regla del contrato (<c>contratos_agentes.md §3.2</c>,
/// <c>DocumentoEncontrado.HashContenido</c>):
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>
///     <b>Fuentes con archivo binario</b> (hoy solo
///     <see cref="FuenteDocumento.Drive"/>): hash sobre los bytes
///     descargados; identifica la versión exacta del archivo aunque el
///     parser cambie.
///   </description></item>
///   <item><description>
///     <b>Fuentes sin archivo binario</b> (Trello, Clockify, MSProject
///     en v1): hash sobre el texto canónico ya normalizado por
///     <see cref="TextNormalizer"/>.
///   </description></item>
/// </list>
/// <para>
/// Centralizado en una clase para no esparcir el <c>switch</c> por fuente
/// entre el agente y futuros consumidores.
/// </para>
/// </remarks>
public static class DocumentHashStrategy
{
    public static bool TieneArtefactoBinario(FuenteDocumento fuente) =>
        fuente switch
        {
            FuenteDocumento.Drive => true,
            FuenteDocumento.Trello => false,
            FuenteDocumento.Clockify => false,
            FuenteDocumento.MSProject => false,
            _ => false
        };

    /// <summary>
    /// Calcula el hash del documento delegando en
    /// <see cref="ContentHasher"/> según la regla por fuente.
    /// </summary>
    /// <param name="fuente">Fuente del documento.</param>
    /// <param name="contenidoBinario">
    /// Stream con los bytes descargados; debe ser seekable y empezar en
    /// posición 0. Si el documento no tiene artefacto binario,
    /// <paramref name="contenidoBinario"/> puede ignorarse.
    /// </param>
    /// <param name="textoNormalizado">
    /// Texto ya normalizado (UTF-8/NFC); se usa cuando la fuente no
    /// tiene artefacto binario.
    /// </param>
    public static async Task<string> ComputeAsync(
        FuenteDocumento fuente,
        Stream contenidoBinario,
        string textoNormalizado,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contenidoBinario);
        ArgumentNullException.ThrowIfNull(textoNormalizado);

        if (TieneArtefactoBinario(fuente))
        {
            return await ContentHasher
                .ComputeSha256OfStreamAsync(contenidoBinario, cancellationToken)
                .ConfigureAwait(false);
        }

        return ContentHasher.ComputeSha256OfText(textoNormalizado);
    }
}
