using ISOAuditAgent.Contracts;
using ISOAuditAgent.DocumentAnalysis.Parsing;

namespace ISOAuditAgent.DocumentAnalysis.Runner;

/// <summary>
/// Política única para calcular <see cref="DocumentoExtraido.HashContenido"/>
/// según la regla del contrato (<c>Contratos_Agentes_Orquestador.md §3.2</c>):
/// </summary>
/// <remarks>
/// <list type="bullet">
///   <item><description>
///     <b>Fuentes con archivo binario</b> (hoy solo
///     <see cref="FuenteDocumento.GoogleDrive"/>): hash sobre los bytes
///     descargados; identifica la versión exacta del archivo aunque el
///     parser cambie.
///   </description></item>
///   <item><description>
///     <b>Fuentes sin archivo binario</b> (Trello, Clockify,
///     Microsoft Project en v1): hash sobre el texto canónico ya
///     normalizado por <see cref="TextNormalizer"/>.
///   </description></item>
/// </list>
/// <para>
/// Centralizado en una clase para no esparcir el <c>switch</c> por
/// fuente entre el runner y futuros consumidores. Cuando se sume MS
/// Project hay que revisitar (.mpp es binario, así que probablemente
/// pase a la rama de bytes).
/// </para>
/// </remarks>
internal static class DocumentHashStrategy
{
    public static bool TieneArtefactoBinario(FuenteDocumento fuente) =>
        fuente switch
        {
            FuenteDocumento.GoogleDrive => true,
            FuenteDocumento.Trello => false,
            FuenteDocumento.Clockify => false,
            FuenteDocumento.MicrosoftProject => false,
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
