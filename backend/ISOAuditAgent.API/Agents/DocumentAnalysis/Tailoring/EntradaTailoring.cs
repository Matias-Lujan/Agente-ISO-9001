namespace ISOAuditAgent.DocumentAnalysis.Tailoring;

/// <summary>
/// Una fila del tailoring operativo (FR 29) ya mapeada desde la planilla
/// del proyecto en Drive.
/// </summary>
/// <param name="CodigoArtefacto">
/// Código de negocio del artefacto (p. ej. FR-30, FR 32).
/// </param>
/// <param name="EtapaNombre">
/// Nombre o etiqueta de etapa declarada en la planilla (crudo; el cruce con
/// <c>Etapa</c> en BD ocurre en fases posteriores).
/// </param>
/// <param name="Aplica">
/// <c>true</c> / <c>false</c> si la celda es inequívoca; <c>null</c> si
/// está vacía o no se pudo interpretar (equivale a tailoring no declarado
/// para esa fila hasta que el LLM refine en Fase F).
/// </param>
/// <param name="JustificacionNoAplica">Texto de justificación si no aplica.</param>
/// <param name="UrlReferencia">
/// URL o enlace declarado en el tailoring hacia el artefacto en Drive u otra fuente.
/// </param>
public sealed record EntradaTailoring(
    string CodigoArtefacto,
    string? EtapaNombre,
    bool? Aplica,
    string? JustificacionNoAplica,
    string? UrlReferencia);
