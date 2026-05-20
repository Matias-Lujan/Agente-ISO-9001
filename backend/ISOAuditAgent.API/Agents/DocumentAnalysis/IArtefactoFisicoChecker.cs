// ============================================================================
//  IArtefactoFisicoChecker — DocumentAnalysis (Parte 4.C)
// ----------------------------------------------------------------------------
//  Abstracción que verifica la existencia física de UN artefacto: descarga el
//  archivo, calcula su hash y parsea sus secciones. Vive como interfaz dentro
//  del agente; la implementación concreta es responsabilidad de Chat D §5.3.
//
//  DECISIÓN ARQUITECTÓNICA — C2:
//  El razonamiento sobre tailoring vs contexto lo hace el LLM. La descarga +
//  hash + parseo de secciones lo hace código C# determinístico (esta
//  interfaz). Aceptado por el equipo: hash y secciones son tareas
//  determinísticas, pedírselas al LLM aumenta el riesgo de alucinación.
//
//  NOTA PARA CHAT D:
//   - La implementación usa los MCP servers según la fuente (Drive en el MVP).
//   - Para Drive: descarga + parsers locales (PdfPig / OpenXML / ClosedXML)
//     según extensión, coherente con arquitectura_software.md §3.3
//     (IDocumentParser). Compara las secciones del documento contra las del
//     template provisto en pathTemplateAbsoluto (también con OpenXML).
//   - Hash: SHA-256 hex en minúsculas, 64 caracteres. El validator del
//     contrato 3 lo verifica.
//   - Registrarla como Scoped en DI (Chat D §5.5).
//   - MVP — solo Drive: aunque la firma admite cualquier FuenteDocumento, en
//     el MVP la implementación siempre devuelve Fuente = Drive. Trello,
//     Clockify y MSProject quedan post-MVP (ver InvariantsValidator).
//
//  ¿POR QUÉ POR ARTEFACTO Y NO POR LOTE?
//   Para mantener la lógica del builder simple (un foreach con un await por
//   artefacto). La implementación puede paralelizar internamente si quiere
//   (Task.WhenAll en su capa), pero el contrato es por artefacto.
//
//  ¿RECIBE URL DEL TAILORING O CÓDIGO?
//   Ambos. El tailoring puede traer una URL específica (prioritaria); si no,
//   la implementación cae a una búsqueda heurística por código de artefacto
//   en el folder del proyecto. La política exacta de búsqueda es decisión de
//   la implementación.
//
//  ¿POR QUÉ NO DEPENDE DE driveFolderId DIRECTAMENTE?
//   La firma recibe IntegracionesProyecto entero. Eso desacopla al agente
//   de la fuente: si mañana se agrega un artefacto en Trello o Clockify, el
//   checker tiene los IDs disponibles. En el MVP siempre se usa DriveFolderId,
//   pero el contrato no se cierra a Drive.
// ============================================================================

using ISOAuditAgent.API.Agents.Contracts;
using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

/// <summary>
/// Verifica la existencia física de un artefacto y extrae sus secciones.
/// Implementación real en Chat D §5.3.
/// </summary>
public interface IArtefactoFisicoChecker
{
    /// <summary>
    /// Intenta localizar y procesar el archivo correspondiente a un artefacto.
    /// </summary>
    /// <param name="integraciones">
    /// IDs de las integraciones externas del proyecto, del contrato 2. La
    /// implementación elige cuál usar según el artefacto (en MVP siempre Drive).
    /// </param>
    /// <param name="artefactoEsperadoId">
    /// ID del artefacto. Solo para logging y mensajes de error; no afecta la
    /// lógica.
    /// </param>
    /// <param name="codigoArtefacto">
    /// Código del artefacto (ej. "FR-30"). Usado por la implementación para
    /// búsqueda heurística cuando no hay URL. Puede ser null si el artefacto
    /// no tiene código formal — en ese caso la implementación cae a búsqueda
    /// por nombre.
    /// </param>
    /// <param name="nombreArtefacto">
    /// Nombre del artefacto (ej. "Cronograma", "Proyecto en Trello"). Fallback
    /// para búsqueda heurística cuando codigoArtefacto es null.
    /// </param>
    /// <param name="urlReferenciaTailoring">
    /// URL declarada en el tailoring, si existe. Prioritaria sobre búsqueda
    /// por código o nombre.
    /// </param>
    /// <param name="pathTemplateAbsoluto">
    /// Ruta absoluta al template del artefacto (ya resuelta por
    /// ResolutorContexto). null cuando el artefacto no tiene template. La
    /// implementación la usa para extraer las secciones esperadas y
    /// compararlas contra las del documento encontrado.
    /// </param>
    /// <returns>
    /// El resultado de la verificación. Si el archivo no se encuentra,
    /// <see cref="VerificacionFisica.Encontrado"/> = false y los demás campos
    /// son null / lista vacía.
    /// </returns>
    ValueTask<VerificacionFisica> VerificarAsync(
        IntegracionesProyecto integraciones,
        int artefactoEsperadoId,
        string? codigoArtefacto,
        string nombreArtefacto,
        string? urlReferenciaTailoring,
        string? pathTemplateAbsoluto,
        CancellationToken ct);
}

/// <summary>
/// Resultado de la verificación física de un artefacto.
///
/// INVARIANTES (las verifica ArtefactosBuilder, falla ruidoso ante inconsistencias):
///  - Encontrado = true  ⇒ NombreArchivo, HashContenido NO null/vacíos; Fuente
///    indica de qué fuente provino. Secciones puede ser lista vacía si el
///    documento no tiene template asociado.
///  - Encontrado = false ⇒ NombreArchivo, HashContenido null; Secciones lista
///    vacía. Fuente es la fuente que se intentó (informativo para errores).
/// </summary>
public sealed record VerificacionFisica(
    bool Encontrado,
    FuenteDocumento Fuente,
    string? NombreArchivo,
    string? HashContenido,
    IReadOnlyList<SeccionDetectada> Secciones);
