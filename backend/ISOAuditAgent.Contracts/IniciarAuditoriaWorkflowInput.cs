namespace ISOAuditAgent.Contracts;

/// <summary>
/// Contrato 1 — Input del workflow del orquestador.
/// Producido por la API REST (POST /api/auditorias) y consumido por el primer
/// nodo del workflow (DocumentAnalysis).
/// </summary>
/// <remarks>
/// Definición alineada con <c>Contratos_Agentes_Orquestador.md</c> §3.1.
/// Pendiente de validar con el cliente la inclusión de
/// <c>IReadOnlyCollection&lt;int&gt;? ProcesoIds</c> si las auditorías
/// admiten alcance variable.
/// </remarks>
public sealed record IniciarAuditoriaWorkflowInput(
    int AuditoriaId,
    int ProyectoId,
    int UsuarioEjecutorId,
    DateTimeOffset FechaInicioUtc
);
