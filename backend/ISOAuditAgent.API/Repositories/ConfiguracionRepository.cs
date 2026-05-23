// ============================================================================
//  ConfiguracionRepository — Acceso key-value a ConfiguracionSistema
// ----------------------------------------------------------------------------
//  Implementa IConfiguracionRepository. En el MVP se usa para resolver
//  'path_carpeta_templates' (consultas_cliente.md, consulta 1).
//
//  La clave 'path_carpeta_templates' viene sembrada por OnModelCreating del
//  DbContext con valor "./templates". El repo solo lee; el seeding ya se hizo
//  en la migración InitialCreate.
// ============================================================================

using ISOAuditAgent.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

public sealed class ConfiguracionRepository : IConfiguracionRepository
{
    private readonly ISOAuditAgentDbContext _context;

    public ConfiguracionRepository(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<string?> ObtenerValorOrNullAsync(string clave, CancellationToken ct)
    {
        return await _context.ConfiguracionesSistema
            .AsNoTracking()
            .Where(c => c.Clave == clave)
            .Select(c => c.Valor)
            .FirstOrDefaultAsync(ct);
    }
}
