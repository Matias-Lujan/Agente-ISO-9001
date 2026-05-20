namespace ISOAuditAgent.Infrastructure.Entities;

/// <summary>
/// Definición de un artefacto que se espera en una etapa de un
/// procedimiento (ej. ERS en Análisis y Diseño, Casos de Prueba en
/// Testing). Indica su código formal cuando aplica (FR 30, FR 11, …),
/// si es mandatorio según el tipo de proyecto (A o B) y dónde está su
/// template.
/// </summary>
/// <remarks>
/// <para>
/// Modelo alineado con <c>modelo_bd.md</c> §3 (tabla
/// <c>ArtefactoEsperado</c>).
/// </para>
/// <para>
/// <b>Templates en Drive:</b> en lugar de un path local
/// (<c>path_template_relativo</c>) el modelo guarda
/// <see cref="TemplateDriveFilename"/> (nombre del archivo dentro de la
/// carpeta única de templates en Drive). La carpeta vive en
/// <c>ConfiguracionSistema.drive_carpeta_templates_id</c>. DocumentAnalysis
/// compone ambos para resolver el <c>FileId</c> real y emitirlo en
/// <c>ArtefactoExtraido.PathTemplateAbsoluto</c> del contrato del agente.
/// </para>
/// </remarks>
public sealed class ArtefactoEsperado
{
    public int Id { get; set; }
    public int EtapaId { get; set; }

    /// <summary>
    /// Código formal del artefacto si aplica (ej. "FR 30", "FR 11").
    /// Nullable porque hay artefactos sin código (ej. cronograma .mpp,
    /// tarjetas de Trello).
    /// </summary>
    public string? Codigo { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    /// <summary>
    /// Indica si es obligatorio para proyectos tipo A (&gt; 1200 horas).
    /// Si es false, es "evaluar y justificar".
    /// </summary>
    public bool MandatorioTipoA { get; set; }

    /// <summary>
    /// Indica si es obligatorio para proyectos tipo B (≤ 1200 horas).
    /// Si es false, es "evaluar y justificar".
    /// </summary>
    public bool MandatorioTipoB { get; set; }

    /// <summary>
    /// Nombre del archivo del template en Drive (ej. <c>"FR-30.docx"</c>).
    /// Nullable porque no todos los artefactos tienen template
    /// (ej. tarjetas de Trello, registros de Clockify). Se compone con
    /// <c>ConfiguracionSistema.drive_carpeta_templates_id</c> para
    /// localizar el archivo real en Drive.
    /// </summary>
    public string? TemplateDriveFilename { get; set; }

    public Etapa? Etapa { get; set; }
}
