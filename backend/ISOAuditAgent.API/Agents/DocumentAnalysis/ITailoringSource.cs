// ============================================================================
//  ITailoringSource — DocumentAnalysis (Parte 4.C)
// ----------------------------------------------------------------------------
//  Abstracción que descarga y parsea el workbook FR 29 (tailoring) del Drive
//  del proyecto. Vive como interfaz dentro del agente; la implementación
//  concreta es responsabilidad de Chat D §5.3 (DI + MCP de Drive + ClosedXML).
//
//  NOTA PARA CHAT D:
//   - La implementación debe usar el MCP server de Drive (no llamadas directas
//     a la Drive API): el plumbing MCP centraliza credenciales y configuración.
//   - El parseo del XLSX usa ClosedXML (decisión arquitectónica documentada
//     en arquitectura_software.md §3.3).
//   - Si el FR-29 no existe en el folder del proyecto, lanzar
//     InvalidOperationException con mensaje claro. El nodo propaga al worker.
//   - Si el FR-29 existe pero está vacío (sin filas), devolver lista vacía.
//     Eso es válido: el LLM matchea cada artefacto contra cero filas y todos
//     quedan SinDeclararEnTailoring.
//   - Implementación: registrarla como Scoped en DI (Chat D §5.5).
//
//  ¿POR QUÉ ESTA INTERFAZ Y NO LLAMADA DIRECTA AL MCP DESDE EL NODO?
//   Para que el nodo no conozca el MCP cliente ni el parser XLSX. Esos son
//   detalles de I/O que viven en otra capa. Es además testeable: en pruebas
//   se mockea esta interfaz con un fake.
//
//  ¿POR QUÉ DEVUELVE FilaTailoring Y NO algo del Models o del contrato?
//   FilaTailoring es un DTO interno del agente: solo lo consume el agente
//   para armar el `tailoring` del user message del LLM. No sale al contrato 3.
//   Vivir cerca de ITailoringSource lo mantiene cohesivo.
// ============================================================================

namespace ISOAuditAgent.API.Agents.DocumentAnalysis;

/// <summary>
/// Descarga el FR-29 (tailoring) del proyecto desde su carpeta de Drive y
/// devuelve las filas interpretables del workbook. Implementación real en
/// Chat D §5.3.
/// </summary>
public interface ITailoringSource
{
    /// <summary>
    /// Obtiene las filas del tailoring del proyecto.
    /// </summary>
    /// <param name="driveFolderId">
    /// ID de la carpeta de Drive del proyecto, viene del contrato 2
    /// (Integraciones.DriveFolderId). El nodo valida que no sea null antes
    /// de llamar.
    /// </param>
    /// <exception cref="InvalidOperationException">
    /// El FR-29 no se encuentra en el folder, o el workbook está corrupto.
    /// </exception>
    ValueTask<TailoringExtraido> ObtenerAsync(
        string driveFolderId,
        CancellationToken ct);
}

/// <summary>
/// Resultado de leer el FR-29: las filas de la tabla + datos de la portada que
/// no son por-artefacto. Hoy lleva el Responsable del proyecto (campo único de
/// la portada). Contrato interno del agente — no sale al contrato 3 tal cual.
/// </summary>
public sealed record TailoringExtraido(
    /// <summary>
    /// Responsable del proyecto declarado en la portada del tailoring (campo
    /// "Responsable:"). null/vacío si no está cargado. NO confundir con la
    /// columna "Responsable" por artefacto de la tabla.
    /// </summary>
    string? ResponsableProyecto,
    IReadOnlyList<FilaTailoring> Filas);

/// <summary>
/// Una fila del FR-29 ya parseada. Contrato interno del agente — no sale al
/// contrato 3. El LLM ve este shape serializado a JSON como `tailoring`.
///
/// MATCHING CON ArtefactoEsperadoContexto:
/// Algunos artefactos esperados no tienen código FR formal (ej. "Proyecto en
/// Trello", "Cronograma") — su CodigoArtefacto del contrato 2 es null. Para
/// que esos artefactos se puedan matchear contra el tailoring, FilaTailoring
/// expone tanto código como nombre. El LLM matchea por código cuando exista
/// (más confiable), por nombre como fallback cuando el código sea null en
/// ambos lados.
/// </summary>
public sealed record FilaTailoring(
    /// <summary>
    /// Código del artefacto tal como figura en el workbook ("FR 30", "FR-30",
    /// "FR30", etc.). El LLM aplica tolerancia razonable al matchear.
    /// Puede ser null para artefactos sin código formal (ej. "Cronograma",
    /// "Proyecto en Trello").
    /// </summary>
    string? CodigoArtefacto,

    /// <summary>
    /// Nombre del artefacto tal como figura en el workbook ("ERS",
    /// "Cronograma", "Proyecto en Trello"). Usado como fallback cuando
    /// CodigoArtefacto es null en ambos lados (contexto y tailoring).
    /// </summary>
    string NombreArtefacto,

    /// <summary>
    /// Aplica = Sí (true), No (false), o celda vacía (null).
    /// </summary>
    bool? Aplica,

    /// <summary>
    /// Justificación cuando Aplica = false. Puede ser null si el responsable
    /// del tailoring no la cargó (ese caso lo evaluará ComplianceValidation
    /// como NC-07 vía hallazgo determinístico).
    /// </summary>
    string? JustificacionNoAplica,

    /// <summary>
    /// URL/path declarada en el tailoring para localizar el artefacto en Drive.
    /// Puede ser null. El IArtefactoFisicoChecker la usa con prioridad sobre
    /// el match heurístico por código.
    /// </summary>
    string? UrlReferencia);
