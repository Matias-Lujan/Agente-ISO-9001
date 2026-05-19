using ISOAuditAgent.Contracts;

namespace ISOAuditAgent.DocumentAnalysis.Parsing;

/// <summary>
/// Resultado de parsear un documento crudo (<b>Fase D</b>). Expone
/// <see cref="Secciones"/> alineadas al Contrato 2; ya no se incluye
/// texto completo para transporte fuera del agente.
/// </summary>
/// <remarks>
/// <para>
/// El hash de archivo sigue calculándose sobre los bytes del stream
/// (<see cref="ContentHasher"/>) sin depender de este resultado.
/// </para>
/// <para>
/// <see cref="TextoNormalizado"/> queda reservado solo para diagnóstico
/// opcional y debe ser <c>null</c> en el flujo normal post–Fase D.
/// </para>
/// </remarks>
public sealed record DocumentParseResult(
    FormatoContenido Formato,
    IReadOnlyList<SeccionDetectada> Secciones,
    string? TextoNormalizado = null);
