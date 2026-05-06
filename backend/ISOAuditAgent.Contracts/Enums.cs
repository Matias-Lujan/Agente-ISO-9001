namespace ISOAuditAgent.Contracts;

/// <summary>
/// Origen del documento ingestado por DocumentAnalysis.
/// Alineado con <c>Contratos_Agentes_Orquestador.md</c> §3.2.
/// </summary>
public enum FuenteDocumento
{
    GoogleDrive,
    Trello,
    Clockify,
    MicrosoftProject
}

/// <summary>
/// Formato del <see cref="DocumentoExtraido.ContenidoTextual"/>.
/// Acotado a dos opciones en v1; ampliar si DocumentAnalysis lo requiere.
/// </summary>
public enum FormatoContenido
{
    PlainText,
    Markdown
}
