namespace ISOAuditAgent.Contracts;

/// <summary>
/// Contrato 2 — Salida de DocumentAnalysisAgent.
/// Producido por DocumentAnalysisAgent (RF-02) y consumido por
/// ComplianceValidationAgent y ConsistencyVerificationAgent en paralelo.
/// </summary>
/// <remarks>
/// Definición alineada con <c>Contratos_Agentes_Orquestador.md</c> §3.2.
/// "Documento" se usa en sentido extendido (RF-02): incluye archivos de Drive,
/// tarjetas de Trello y registros de Clockify (estos últimos fuera de alcance v1).
/// </remarks>
public sealed record DocumentosExtraidos(
    int AuditoriaId,
    int ProyectoId,
    IReadOnlyList<DocumentoExtraido> Documentos
);

public sealed record DocumentoExtraido(
    string IdEnFuente,
    string NombreArchivo,
    FuenteDocumento Fuente,
    string? UrlReferencia,
    string ContenidoTextual,
    FormatoContenido FormatoContenido,
    string HashContenido,
    DocumentoMetadatos Metadatos
);

public sealed record DocumentoMetadatos(
    string? Autor,
    DateTimeOffset? FechaCreacion,
    DateTimeOffset? FechaModificacion,
    string? Version,
    string? Estado
);
