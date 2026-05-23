// ============================================================================
//  ITemplateParser — Contrato de los parsers de documentos (D3.3)
// ----------------------------------------------------------------------------
//  Devuelve la lista de secciones del documento. La forma del DTO de salida
//  ya está fijada por el contrato 3 (SeccionDetectada: Titulo + TieneContenido).
//
//  Sincrónico a propósito: OpenXml, ClosedXML y PdfPig son APIs sync que
//  trabajan en memoria. Async ceremonial no aporta. Cuando D3.5 los invoque
//  desde HandleAsync del nodo, ya está en contexto async.
//
//  Sin dispatcher por MIME en D3.3: el caller elige el parser concreto. El
//  switch por MIME aparecerá en D3.5 (ArtefactoFisicoChecker) o en el smoke
//  /api/_smoke/parse-file — donde tenga más sentido funcional.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis.Parsing;

public interface ITemplateParser
{
    /// <summary>
    /// Extrae las secciones del documento dado en bytes.
    /// </summary>
    /// <exception cref="ArgumentNullException">bytes es null.</exception>
    /// <exception cref="InvalidOperationException">
    /// El documento está vacío o no es del formato esperado por este parser.
    /// </exception>
    IReadOnlyList<SeccionDetectada> Parsear(byte[] bytes);
}