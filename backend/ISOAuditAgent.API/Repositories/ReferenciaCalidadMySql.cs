using ISOAuditAgent.API.Data;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementación de <see cref="IReferenciaCalidad"/> sobre la tabla MySQL
/// <c>formularios_calidad</c>. Es la fuente "Departamento de Calidad" del MVP.
///
/// El día que Calidad provea su propia fuente (carpeta/registro en Drive, una
/// API, etc.) se escribe otra implementación de <see cref="IReferenciaCalidad"/>
/// y se cambia el registro DI — sin tocar ResolutorContexto ni el resto.
/// </summary>
public sealed class ReferenciaCalidadMySql : IReferenciaCalidad
{
    private readonly ISOAuditAgentDbContext _context;

    public ReferenciaCalidadMySql(ISOAuditAgentDbContext context)
    {
        _context = context;
    }

    public async Task<ReferenciaCalidadVigencias> ObtenerAsync(CancellationToken ct)
    {
        var filas = await _context.FormulariosCalidad
            .AsNoTracking()
            .Select(f => new { f.CodigoFormulario, f.VigenciaVigente })
            .ToListAsync(ct);

        var porCodigo = new Dictionary<string, DateOnly>(filas.Count);
        foreach (var f in filas)
        {
            var clave = ReferenciaCalidadVigencias.NormalizarCodigo(f.CodigoFormulario);
            if (clave.Length == 0) continue;
            // El índice único de codigo_formulario evita duplicados; ante uno, la
            // última fila gana (comportamiento determinista, no debería ocurrir).
            porCodigo[clave] = f.VigenciaVigente;
        }

        return new ReferenciaCalidadVigencias(porCodigo);
    }
}
