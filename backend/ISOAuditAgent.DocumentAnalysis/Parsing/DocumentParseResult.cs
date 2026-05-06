using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Resultado de parsear un documento crudo (Fase 4). Contiene el texto
/// extraído ya normalizado a UTF-8 + NFC y la indicación del
/// <see cref="FormatoContenido"/> elegido (texto plano para PDF/DOCX,
/// Markdown para XLSX por convención del equipo).
/// </summary>
/// <remarks>
/// El runner (Fase 5) toma este resultado y, junto con los metadatos
/// del <see cref="Sources.RawDocument"/> y el <c>HashContenido</c>
/// calculado por <see cref="ContentHasher"/>, ensambla el
/// <see cref="DocumentoExtraido"/> definitivo del contrato del
/// orquestador.
/// </remarks>
public sealed record DocumentParseResult(
    string TextoNormalizado,
    FormatoContenido Formato);
