// ============================================================================
//  Contrato 3 � Salida de DocumentAnalysis (contratos_agentes.md v2.2)
// ----------------------------------------------------------------------------
//  Lo produce DocumentAnalysis: recibe el ContextoAuditoria (artefactos
//  esperados ya enriquecidos), lee el tailoring del proyecto, lo cruza con
//  los artefactos esperados y va a buscar los exigibles que aplican.
//
//  Lo consumen ComplianceValidation y ConsistencyVerification en paralelo
//  (fan-out), y tambi�n FindingsClassification v�a edge directo.
//
//  DESV�O REGISTRADO respecto de contratos_agentes.md v2.2:
//  La spec v2.2 define el campo de ArtefactoExtraido como
//  'EstadoTailoring EstadoTailoring', con un enum propio EstadoTailoring.
//  Por la decisi�n cerrada 2.8 del plan maestro de integraci�n, el enum
//  EstadoTailoring se ELIMINA y se unifica en el enum compartido
//  EstadoAplicacionTailoring (definido en ISOAuditAgent.API.Models, junto a
//  las entidades EF). El campo pasa a 'EstadoAplicacionTailoring
//  EstadoAplicacionTailoring'. La spec v2.2 queda desactualizada en este
//  punto.
//
//  Enums:
//   - EstadoDisponibilidad: propio del workflow, se define ac�.
//   - ExigibilidadArtefacto / ObligatoriedadArtefacto: definidos en
//     ContextoAuditoria.cs (contrato 2), se referencian.
//   - EstadoAplicacionTailoring / FuenteDocumento: enums compartidos con las
//     entidades EF, viven en ISOAuditAgent.API.Models.
// ============================================================================

using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Agents.Contracts;

// ----------------------------------------------------------------------------
//  DTO principal
// ----------------------------------------------------------------------------

/// <summary>
/// Salida de DocumentAnalysis. Incluye TODOS los artefactos esperados del
/// procedimiento � exigibles y PendienteEtapaFutura � para que el dashboard
/// pueda calcular el porcentaje de cumplimiento sobre el total.
/// </summary>
public sealed record DocumentosExtraidos(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    string ProcedimientoCodigo,
    string ProcedimientoNombre,
    IReadOnlyList<ArtefactoExtraido> Artefactos
);

/// <summary>
/// Resultado por artefacto esperado del procedimiento: clasificado por
/// exigibilidad y obligatoriedad y, si aplica, con el documento f�sico
/// encontrado en la fuente externa.
/// </summary>
public sealed record ArtefactoExtraido(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,
    string NombreArtefacto,
    int EtapaArtefactoId,

    // Pre-resueltos por ResolutorContexto; DocumentAnalysis los propaga.
    ExigibilidadArtefacto Exigibilidad,
    ObligatoriedadArtefacto Obligatoriedad,

    // Voz del tailoring. Enum unificado (ver desv�o en la cabecera).
    EstadoAplicacionTailoring EstadoAplicacionTailoring,
    string? JustificacionNoAplica,

    // Resultado de la recolecci�n f�sica del artefacto.
    EstadoDisponibilidad EstadoDisponibilidad,
    string? UrlReferencia,
    string? NombreTemplateArchivo,
    DocumentoEncontrado? DocumentoEncontrado,
    IReadOnlyList<SeccionDetectada> SeccionesDetectadas,
    IReadOnlyList<SeccionDetectada> SeccionesTemplate,

    /// <summary>
    /// Propósito del artefacto según el procedimiento (propagado desde
    /// ArtefactoEsperadoContexto). Lo usan ConsistencyVerification y
    /// FindingsClassification como anclaje para juzgar la esencialidad de una
    /// sección vacía contra para qué sirve el documento. Nullable.
    /// </summary>
    string? Descripcion,

    /// <summary>
    /// Vigencia detectada en el documento real (etiqueta "Vigencia" en
    /// header/footer del FR). La extrae el parser en DocumentAnalysis. null si el
    /// documento no se encontró, el formato no es parseable, o no se detectó la
    /// etiqueta.
    /// </summary>
    DateOnly? VigenciaDetectada,

    /// <summary>
    /// Vigencia esperada del formulario según Calidad, propagada desde el
    /// contrato 2. null si Calidad no tiene entrada para este FR.
    /// </summary>
    DateOnly? VigenciaEsperada,

    /// <summary>
    /// true si el documento encontrado pudo parsearse (formato soportado:
    /// docx/xlsx/pdf). Distingue "parseable pero sin Vigencia detectada" (→ OBS)
    /// de "formato no soportado" (→ solo log). false cuando no hay documento.
    /// </summary>
    bool DocumentoParseable
);

/// <summary>
/// Documento f�sico efectivamente recolectado desde una fuente externa.
/// Solo se construye cuando EstadoDisponibilidad = Encontrado.
/// El HashContenido se calcula sobre los bytes (fuentes binarias) o sobre el
/// texto can�nico normalizado (fuentes sin binario).
/// </summary>
public sealed record DocumentoEncontrado(
    string NombreArchivo,
    FuenteDocumento Fuente,
    string HashContenido
);

/// <summary>
/// Secci�n detectada dentro del documento encontrado, comparable contra el
/// template del FR correspondiente. TieneContenido indica si la secci�n tiene
/// alg�n contenido m�s all� del propio t�tulo.
/// </summary>
public sealed record SeccionDetectada(
    string Titulo,
    bool TieneContenido
);

// ----------------------------------------------------------------------------
//  Enum propio del workflow � definido una sola vez, ac�
// ----------------------------------------------------------------------------

/// <summary>
/// Resultado de la recolecci�n f�sica del artefacto. Solo los artefactos
/// exigibles que aplican se buscan; el resto queda en NoBuscado.
/// </summary>
public enum EstadoDisponibilidad
{
    Encontrado,
    Faltante,
    NoBuscado,
    /// <summary>
    /// El archivo existe en Drive pero no pudo exportarse (formato nativo Google,
    /// ej: supera el límite de 10 MB de la API). El contenido no está disponible
    /// para verificación; se genera un hallazgo determinístico que requiere
    /// revisión manual.
    /// </summary>
    ExportFallido
}