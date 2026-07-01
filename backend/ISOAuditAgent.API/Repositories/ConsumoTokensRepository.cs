using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementación EF + MySQL del repositorio de consumo de tokens.
/// </summary>
public class ConsumoTokensRepository : IConsumoTokensRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<ConsumoTokensRepository> _logger;

    public ConsumoTokensRepository(
        ISOAuditAgentDbContext db, ILogger<ConsumoTokensRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task GuardarLoteAsync(IEnumerable<ConsumoTokens> registros, CancellationToken ct)
    {
        var lista = registros as ICollection<ConsumoTokens> ?? registros.ToList();
        if (lista.Count == 0)
            return;

        _db.ConsumosTokens.AddRange(lista);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<ResumenConsumoTokens> ObtenerResumenAsync(CancellationToken ct)
    {
        try
        {
            // Traemos las filas con una proyección simple (columnas, sin agregar)
            // y agrupamos/sumamos en memoria. La tabla es chica (unas pocas filas
            // por auditoría), y así evitamos el GroupBy + agregados traducido a
            // SQL, que con este provider no traducía y devolvía el resumen vacío.
            var filas = await _db.ConsumosTokens
                .Select(c => new
                {
                    c.AgenteKey,
                    c.TokensEntrada,
                    c.TokensSalida,
                    c.TokensTotal,
                    c.AuditoriaId,
                })
                .ToListAsync(ct);

            // Desglose por agente (en memoria).
            var porAgente = filas
                .GroupBy(f => f.AgenteKey)
                .Select(g => new ConsumoPorAgente(
                    g.Key,
                    g.Sum(f => (long)f.TokensEntrada),
                    g.Sum(f => (long)f.TokensSalida),
                    g.Sum(f => (long)f.TokensTotal),
                    g.Count()))
                .OrderByDescending(a => a.TokensTotal)
                .ToList();

            var cantidadAuditorias = filas
                .Where(f => f.AuditoriaId != null)
                .Select(f => f.AuditoriaId)
                .Distinct()
                .Count();

            var total = new ConsumoTotal(
                porAgente.Sum(a => a.TokensEntrada),
                porAgente.Sum(a => a.TokensSalida),
                porAgente.Sum(a => a.TokensTotal),
                porAgente.Sum(a => a.CantidadLlamadas),
                cantidadAuditorias);

            return new ResumenConsumoTokens(total, porAgente);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener el resumen de consumo de tokens.");
            return new ResumenConsumoTokens(
                new ConsumoTotal(0, 0, 0, 0, 0), []);
        }
    }
}
