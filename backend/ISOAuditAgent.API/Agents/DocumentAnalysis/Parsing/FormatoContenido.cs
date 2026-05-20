namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Formato del texto producido por un <see cref="IDocumentParser"/>.
/// </summary>
/// <remarks>
/// Tipo interno del módulo de parsing: NO forma parte del contrato externo
/// del agente. DocumentAnalysis no transporta texto completo fuera del
/// proceso (ver <c>contratos_agentes.md</c> §3.2: "el sistema valida
/// existencia y estructura, no contenido"). El formato es relevante
/// solamente dentro del módulo, para que cada parser declare cómo está
/// renderizando los bytes que leyó.
/// </remarks>
public enum FormatoContenido
{
    PlainText,
    Markdown
}
