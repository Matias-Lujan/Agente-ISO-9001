using System.Diagnostics.CodeAnalysis;
using ISOAuditAgent.DocumentAnalysis.Drive;
using ISOAuditAgent.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.Infrastructure.Drive;

/// <summary>
/// Implementación de <see cref="IProyectoDriveResolver"/> que resuelve la
/// carpeta de Drive consultando la tabla <c>proyecto</c> de MySQL.
/// </summary>
/// <remarks>
/// <para>
/// Reemplaza la implementación basada en <c>appsettings.json</c>
/// (<see cref="ISOAuditAgent.DocumentAnalysis.Drive.OptionsBackedProyectoDriveResolver"/>)
/// una vez que la BD esté disponible. Mientras la migración no se aplique,
/// el resolver se construye pero no se registra como default en DI
/// (Fase B del plan de desarrollo).
/// </para>
/// <para>
/// La resolución es síncrona porque la interfaz lo es. Internamente
/// ejecuta una consulta bloqueante; está pensada para usos de bajo
/// volumen (una vez por auditoría). Si en el futuro se necesita escala,
/// la interfaz se puede ampliar a una versión async sin tocar a sus
/// consumidores actuales.
/// </para>
/// </remarks>
public sealed class DbBackedProyectoDriveResolver : IProyectoDriveResolver
{
    private readonly ISOAuditAgentDbContext _db;

    public DbBackedProyectoDriveResolver(ISOAuditAgentDbContext db)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
    }

    public string ResolveFolderId(int proyectoId)
    {
        if (TryResolveFolderId(proyectoId, out var folderId))
        {
            return folderId;
        }

        throw new ProyectoDriveMappingNotFoundException(proyectoId);
    }

    public bool TryResolveFolderId(int proyectoId, [NotNullWhen(true)] out string? folderId)
    {
        // SingleOrDefault devuelve string?; el filtro Activo evita
        // que un proyecto deshabilitado quede mapeado por accidente.
        folderId = _db.Proyectos
            .AsNoTracking()
            .Where(p => p.Id == proyectoId && p.Activo && p.DriveFolderId != null)
            .Select(p => p.DriveFolderId!)
            .SingleOrDefault();

        return folderId is not null;
    }
}
