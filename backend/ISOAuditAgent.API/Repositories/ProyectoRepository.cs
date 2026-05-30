using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Repositories;

/// <summary>
/// Implementacion del repositorio de proyectos usando Entity Framework + MySQL.
/// </summary>
public class ProyectoRepository : IProyectoRepository
{
    private readonly ISOAuditAgentDbContext _db;
    private readonly ILogger<ProyectoRepository> _logger;

    public ProyectoRepository(ISOAuditAgentDbContext db, ILogger<ProyectoRepository> logger)
    {
        _db     = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Proyecto>> ObtenerTodosAsync()
    {
        try
        {
            return await _db.Proyectos
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener todos los proyectos");
            return [];
        }
    }

    public async Task<Proyecto?> ObtenerPorIdAsync(int id)
    {
        try
        {
            return await _db.Proyectos
                .FirstOrDefaultAsync(p => p.Id == id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proyecto por ID: {Id}", id);
            return null;
        }
    }

    public async Task<IReadOnlyList<Proyecto>> ObtenerPorUsuarioAsync(int usuarioId)
    {
        try
        {
            return await _db.ProyectosUsuarios
                .Where(pu => pu.UsuarioId == usuarioId)
                .Select(pu => pu.Proyecto)
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener proyectos por usuario: {Id}", usuarioId);
            return [];
        }
    }

    public async Task<Proyecto> CrearAsync(Proyecto proyecto)
    {
        _db.Proyectos.Add(proyecto);
        await _db.SaveChangesAsync();
        return proyecto;
    }

    public async Task<Proyecto> ActualizarAsync(Proyecto proyecto)
    {
        _db.Proyectos.Update(proyecto);
        await _db.SaveChangesAsync();
        return proyecto;
    }

    public async Task AsignarResponsableAsync(int proyectoId, int usuarioId)
    {
        // Verificar que no exista ya la asignacion
        var existe = await _db.ProyectosUsuarios
            .AnyAsync(pu => pu.ProyectoId == proyectoId && pu.UsuarioId == usuarioId);

        if (existe) return;

        _db.ProyectosUsuarios.Add(new ProyectoUsuario
        {
            ProyectoId = proyectoId,
            UsuarioId  = usuarioId
        });
        await _db.SaveChangesAsync();
    }

    public async Task QuitarResponsableAsync(int proyectoId, int usuarioId)
    {
        var asignacion = await _db.ProyectosUsuarios
            .FirstOrDefaultAsync(pu => pu.ProyectoId == proyectoId && pu.UsuarioId == usuarioId);

        if (asignacion == null) return;

        _db.ProyectosUsuarios.Remove(asignacion);
        await _db.SaveChangesAsync();
    }

    public async Task<IReadOnlyList<int>> ObtenerResponsablesAsync(int proyectoId)
    {
        try
        {
            return await _db.ProyectosUsuarios
                .Where(pu => pu.ProyectoId == proyectoId)
                .Select(pu => pu.UsuarioId)
                .ToListAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error al obtener responsables del proyecto: {Id}", proyectoId);
            return [];
        }
    }
}