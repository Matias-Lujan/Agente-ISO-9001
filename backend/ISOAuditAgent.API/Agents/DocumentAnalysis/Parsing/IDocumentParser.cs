namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Contrato común de los parsers de DocumentAnalysis (Fase 4 + Fase D). Cada
/// implementación maneja uno o más MIME types y produce un
/// <see cref="DocumentParseResult"/> con
/// <see cref="DocumentParseResult.Secciones"/>; el texto completo ya no se
/// incluye en el flujo normal del agente (<see cref="DocumentParseResult.TextoNormalizado"/>
/// queda en <c>null</c>).
/// </summary>
/// <remarks>
/// <para>
/// Las implementaciones se registran como múltiples
/// <see cref="IDocumentParser"/> en DI; el despacho por MIME lo hace
/// <see cref="DocumentParserRegistry"/>.
/// </para>
/// <para>
/// El <see cref="Stream"/> recibido se lee desde la posición actual; el
/// caller se encarga de dejarlo en <c>Position = 0</c> (los streams de
/// <see cref="Sources.RawDocument"/> ya cumplen ese contrato).
/// </para>
/// </remarks>
public interface IDocumentParser
{
    /// <summary>
    /// MIME types que este parser sabe manejar (ej. "application/pdf").
    /// </summary>
    IReadOnlyCollection<string> SupportedMimeTypes { get; }

    /// <summary>
    /// Extrae la estructura por secciones del stream (Fase D). El texto
    /// completo no se devuelve; usar solo <see cref="DocumentParseResult.Secciones"/>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Falla de parseo (PDF corrupto, DOCX no estándar, etc.). El
    /// mensaje incluye contexto del nombre del archivo.
    /// </exception>
    Task<DocumentParseResult> ParseAsync(
        Stream contenido,
        string nombreArchivo,
        CancellationToken cancellationToken = default);
}
