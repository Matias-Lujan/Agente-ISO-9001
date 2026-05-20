using System.Diagnostics.CodeAnalysis;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Despacha la decisión "qué <see cref="IDocumentParser"/> aplicar"
/// según el MIME type del documento. Se construye desde la colección
/// de <see cref="IDocumentParser"/> registrados en DI; valida que no
/// haya colisiones (dos parsers para el mismo MIME).
/// </summary>
/// <remarks>
/// Dado un <see cref="Sources.RawDocument"/>, llama
/// <see cref="GetRequiredParser(string)"/> y delega el parseo a
/// <see cref="IDocumentParser.ParseAsync"/> (Fase D: secciones, sin texto completo).
/// </remarks>
public sealed class DocumentParserRegistry
{
    private readonly Dictionary<string, IDocumentParser> _byMime;

    public DocumentParserRegistry(IEnumerable<IDocumentParser> parsers)
    {
        ArgumentNullException.ThrowIfNull(parsers);

        _byMime = new Dictionary<string, IDocumentParser>(StringComparer.OrdinalIgnoreCase);

        foreach (var parser in parsers)
        {
            foreach (var mime in parser.SupportedMimeTypes)
            {
                if (_byMime.TryGetValue(mime, out var existing))
                {
                    throw new InvalidOperationException(
                        $"Conflicto de parsers para MIME '{mime}': " +
                        $"'{existing.GetType().Name}' y '{parser.GetType().Name}'.");
                }

                _byMime[mime] = parser;
            }
        }
    }

    /// <summary>
    /// MIME types con parser registrado.
    /// </summary>
    public IReadOnlyCollection<string> SupportedMimeTypes => _byMime.Keys;

    /// <summary>
    /// Intenta obtener el parser para un MIME type. Devuelve <c>false</c>
    /// si no hay ninguno registrado.
    /// </summary>
    public bool TryGetParser(
        string mimeType,
        [NotNullWhen(true)] out IDocumentParser? parser)
    {
        ArgumentException.ThrowIfNullOrEmpty(mimeType);
        return _byMime.TryGetValue(mimeType, out parser);
    }

    /// <summary>
    /// Devuelve el parser apropiado para <paramref name="mimeType"/> o
    /// lanza <see cref="NotSupportedException"/> si no hay ninguno.
    /// </summary>
    public IDocumentParser GetRequiredParser(string mimeType)
    {
        if (!TryGetParser(mimeType, out var parser))
        {
            throw new NotSupportedException(
                $"No hay parser registrado para MIME type '{mimeType}'.");
        }

        return parser;
    }
}
