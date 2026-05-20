namespace ISOAuditAgent.Contracts;

/// <summary>
/// Contrato 2 — Salida del agente DocumentAnalysis.
/// Producido por DocumentAnalysis (RF-02) tras leer el tailoring del
/// proyecto, cruzarlo con los artefactos esperados del procedimiento +
/// etapa y recolectar los exigibles que aplican. Consumido por
/// ComplianceValidation y ConsistencyVerification en paralelo (fan-out)
/// y por <c>ConsolidadorResultado</c> al final del workflow vía edge
/// directo.
/// </summary>
/// <remarks>
/// <para>
/// Definición alineada con <c>contratos_agentes.md §3.2</c> (v2.1).
/// </para>
/// <para>
/// <b>Invariantes que DocumentAnalysis debe respetar al construir el DTO</b>:
/// </para>
/// <list type="bullet">
///   <item><description>
///     Si <c>EstadoDisponibilidad = Encontrado</c> →
///     <c>DocumentoEncontrado</c> no es null.
///   </description></item>
///   <item><description>
///     Si <c>EstadoDisponibilidad ∈ {Faltante, NoBuscado}</c> →
///     <c>DocumentoEncontrado</c> es null.
///   </description></item>
///   <item><description>
///     Si <c>EstadoTailoring = NoAplica</c> con justificación →
///     <c>JustificacionNoAplica</c> tiene texto. La validez de la
///     justificación la decide ComplianceValidation, no DocumentAnalysis.
///   </description></item>
///   <item><description>
///     Si <c>EstadoTailoring = NoAplica</c> sin justificación →
///     <c>JustificacionNoAplica</c> es null o string vacío.
///   </description></item>
///   <item><description>
///     Si <c>Exigibilidad = PendienteEtapaFutura</c> →
///     <c>EstadoDisponibilidad = NoBuscado</c> y
///     <c>DocumentoEncontrado = null</c>.
///   </description></item>
///   <item><description>
///     Si no aplica template (ej. tarjeta de Trello, registro de
///     Clockify) → <c>PathTemplateAbsoluto = null</c> y
///     <c>SeccionesDetectadas</c> queda vacío.
///   </description></item>
///   <item><description>
///     <c>Faltante</c> solo aplica a artefactos exigibles que se
///     buscaron y no estaban. Nunca a <c>PendienteEtapaFutura</c>.
///   </description></item>
/// </list>
/// <para>
/// <b>No se transporta</b> el texto completo del documento ni metadatos
/// del archivo (autor, fechas). El sistema valida existencia y estructura,
/// no contenido (consultas 2 y 3 al cliente: BDT no firma documentos ni
/// los hace caducar).
/// </para>
/// </remarks>
public sealed record DocumentosExtraidos(
    int AuditoriaId,
    int ProyectoId,
    int EtapaId,
    IReadOnlyList<ArtefactoExtraido> Artefactos
);

/// <summary>
/// Resultado por artefacto esperado del procedimiento + etapa, ya
/// clasificado por exigibilidad y obligatoriedad y, si aplica, con el
/// documento físico encontrado en la fuente externa.
/// </summary>
public sealed record ArtefactoExtraido(
    int ArtefactoEsperadoId,
    string? CodigoArtefacto,
    string NombreArtefacto,
    int EtapaArtefactoId,
    ExigibilidadArtefacto Exigibilidad,
    ObligatoriedadArtefacto Obligatoriedad,
    EstadoTailoring EstadoTailoring,
    string? JustificacionNoAplica,
    EstadoDisponibilidad EstadoDisponibilidad,
    string? UrlReferencia,
    string? PathTemplateAbsoluto,
    DocumentoEncontrado? DocumentoEncontrado,
    IReadOnlyList<SeccionDetectada> SeccionesDetectadas
);

/// <summary>
/// Documento físico efectivamente recolectado desde una fuente externa
/// (Drive en el MVP). Solo se construye cuando
/// <see cref="EstadoDisponibilidad.Encontrado"/>.
/// </summary>
/// <remarks>
/// El <c>HashContenido</c> se calcula sobre los bytes para fuentes
/// binarias (Drive) o sobre el texto canónico normalizado para fuentes
/// sin binario (Trello, Clockify). Política implementada en
/// <c>DocumentHashStrategy</c>.
/// </remarks>
public sealed record DocumentoEncontrado(
    string NombreArchivo,
    FuenteDocumento Fuente,
    string HashContenido
);

/// <summary>
/// Sección detectada dentro del documento encontrado, comparable contra
/// el template del FR correspondiente. <c>TieneContenido</c> indica si la
/// sección tiene algún contenido más allá del propio título.
/// </summary>
public sealed record SeccionDetectada(
    string Titulo,
    bool TieneContenido
);
