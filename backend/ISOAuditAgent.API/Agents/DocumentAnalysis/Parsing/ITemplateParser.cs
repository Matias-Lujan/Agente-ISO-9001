// ============================================================================
//  ITemplateParser � Contrato de los parsers de documentos (D3.3)
// ----------------------------------------------------------------------------
//  Devuelve la lista de secciones del documento. La forma del DTO de salida
//  ya est� fijada por el contrato 3 (SeccionDetectada: Titulo + TieneContenido).
//
//  Sincr�nico a prop�sito: OpenXml, ClosedXML y PdfPig son APIs sync que
//  trabajan en memoria. Async ceremonial no aporta. Cuando D3.5 los invoque
//  desde HandleAsync del nodo, ya est� en contexto async.
//
//  Sin dispatcher por MIME en D3.3: el caller elige el parser concreto. El
//  switch por MIME aparecer� en D3.5 (ArtefactoFisicoChecker) o en el smoke
//  /api/_smoke/parse-file � donde tenga m�s sentido funcional.
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
    /// El documento est� vac�o o no es del formato esperado por este parser.
    /// </exception>
    IReadOnlyList<SeccionDetectada> Parsear(byte[] bytes);

    /// <summary>
    /// Devuelve los bloques de texto donde puede estar la fecha de Vigencia,
    /// según el formato: DOCX → cada header, cada footer y el cuerpo; XLSX → el
    /// header/footer de impresión de cada hoja y sus celdas; PDF → cada página.
    ///
    /// La DETECCIÓN del label "Vigencia" + fecha la hace <c>VigenciaScanner</c>
    /// sobre estos bloques. Separar "extraer texto" (acá) de "detectar" (scanner)
    /// permite, cuando no se encuentra la vigencia, loguear EXACTAMENTE qué texto
    /// se escaneó — y así diagnosticar cualquier documento sin adivinar.
    ///
    /// NO lanza: ante cualquier error devuelve lo que haya podido juntar (o vacío).
    /// </summary>
    IReadOnlyList<string> ExtraerBloquesVigencia(byte[] bytes);
}