using ISOAuditAgent.API.Models;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Total agregado del consumo de tokens de la app.
/// </summary>
public record ConsumoTotal(
    long TokensEntrada,
    long TokensSalida,
    long TokensTotal,
    int CantidadLlamadas,
    int CantidadAuditorias);

/// <summary>
/// Consumo agregado de un agente en particular.
/// </summary>
public record ConsumoPorAgente(
    string AgenteKey,
    long TokensEntrada,
    long TokensSalida,
    long TokensTotal,
    int CantidadLlamadas);

/// <summary>
/// Resumen del consumo: total + desglose por agente.
/// </summary>
public record ResumenConsumoTokens(
    ConsumoTotal Total,
    IReadOnlyList<ConsumoPorAgente> PorAgente);

/// <summary>
/// Repositorio del consumo de tokens del LLM. Escribe los registros que acumula
/// cada auditoría y agrega el resumen para el KPI de Configuración.
/// </summary>
public interface IConsumoTokensRepository
{
    /// <summary>Persiste un lote de registros de consumo (flush de una auditoría).</summary>
    Task GuardarLoteAsync(IEnumerable<ConsumoTokens> registros, CancellationToken ct);

    /// <summary>Devuelve el consumo total de la app + el desglose por agente.</summary>
    Task<ResumenConsumoTokens> ObtenerResumenAsync(CancellationToken ct);
}
