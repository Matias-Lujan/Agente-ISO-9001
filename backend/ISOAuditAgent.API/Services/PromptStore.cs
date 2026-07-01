using ISOAuditAgent.API.Agents.DocumentAnalysis;
using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Services;

/// <summary>
/// Acceso a los system prompts versionados de los agentes. El prompt en uso para
/// un agente es la versión activa en BD; si no hay ninguna (nunca se sembró),
/// cae al default en código (SystemPrompts.Defaults). Guardar/restablecer/revertir
/// insertan una versión nueva (append-only) — nunca se pierde el historial.
/// </summary>
public interface IPromptStore
{
    /// <summary>Contenido del prompt activo. Sync: lo consume el factory del AIAgent.</summary>
    string ObtenerActivo(string agenteKey);

    /// <summary>Contenido del prompt activo (async, para controllers).</summary>
    Task<string> ObtenerActivoAsync(string agenteKey, CancellationToken ct = default);

    /// <summary>Historial de versiones de un agente, de la más nueva a la más vieja.</summary>
    Task<IReadOnlyList<PromptAgente>> ObtenerHistorialAsync(string agenteKey, CancellationToken ct = default);

    /// <summary>Inserta una versión nueva y la deja activa.</summary>
    Task<PromptAgente> GuardarNuevaVersionAsync(
        string agenteKey, string contenido, int? usuarioId, string? comentario, CancellationToken ct = default);

    /// <summary>Inserta una versión nueva con el default en código.</summary>
    Task<PromptAgente> RestablecerDefaultAsync(string agenteKey, int? usuarioId, CancellationToken ct = default);

    /// <summary>Inserta una versión nueva copiando el contenido de una versión previa.</summary>
    Task<PromptAgente> RevertirAsync(string agenteKey, int version, int? usuarioId, CancellationToken ct = default);
}

public class PromptStore : IPromptStore
{
    private readonly ISOAuditAgentDbContext _db;

    public PromptStore(ISOAuditAgentDbContext db) => _db = db;

    private static void ValidarKey(string agenteKey)
    {
        if (!SystemPrompts.Defaults.ContainsKey(agenteKey))
            throw new ArgumentException($"Agente desconocido: '{agenteKey}'.");
    }

    private static string Default(string agenteKey) => SystemPrompts.Defaults[agenteKey];

    public string ObtenerActivo(string agenteKey)
    {
        ValidarKey(agenteKey);
        var contenido = _db.PromptsAgente
            .Where(p => p.AgenteKey == agenteKey && p.EsActiva)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Contenido)
            .FirstOrDefault();
        return contenido ?? Default(agenteKey);
    }

    public async Task<string> ObtenerActivoAsync(string agenteKey, CancellationToken ct = default)
    {
        ValidarKey(agenteKey);
        var contenido = await _db.PromptsAgente
            .Where(p => p.AgenteKey == agenteKey && p.EsActiva)
            .OrderByDescending(p => p.Version)
            .Select(p => p.Contenido)
            .FirstOrDefaultAsync(ct);
        return contenido ?? Default(agenteKey);
    }

    public async Task<IReadOnlyList<PromptAgente>> ObtenerHistorialAsync(
        string agenteKey, CancellationToken ct = default)
    {
        ValidarKey(agenteKey);
        return await _db.PromptsAgente
            .AsNoTracking()
            .Include(p => p.ModificadoPor)
            .Where(p => p.AgenteKey == agenteKey)
            .OrderByDescending(p => p.Version)
            .ToListAsync(ct);
    }

    public async Task<PromptAgente> GuardarNuevaVersionAsync(
        string agenteKey, string contenido, int? usuarioId, string? comentario, CancellationToken ct = default)
    {
        ValidarKey(agenteKey);
        if (string.IsNullOrWhiteSpace(contenido))
            throw new ArgumentException("El contenido del prompt no puede estar vacío.");

        // Desactivar la(s) versión(es) activa(s) actual(es).
        var activas = await _db.PromptsAgente
            .Where(p => p.AgenteKey == agenteKey && p.EsActiva)
            .ToListAsync(ct);
        foreach (var a in activas) a.EsActiva = false;

        var maxVersion = await _db.PromptsAgente
            .Where(p => p.AgenteKey == agenteKey)
            .MaxAsync(p => (int?)p.Version, ct) ?? 0;

        var nueva = new PromptAgente
        {
            AgenteKey = agenteKey,
            Version = maxVersion + 1,
            Contenido = contenido,
            EsActiva = true,
            ModificadoPorUsuarioId = usuarioId,
            FechaCreacion = DateTime.UtcNow,
            Comentario = comentario,
        };

        _db.PromptsAgente.Add(nueva);
        await _db.SaveChangesAsync(ct);
        return nueva;
    }

    public Task<PromptAgente> RestablecerDefaultAsync(
        string agenteKey, int? usuarioId, CancellationToken ct = default)
    {
        ValidarKey(agenteKey);
        return GuardarNuevaVersionAsync(
            agenteKey, Default(agenteKey), usuarioId, "Restablecido al valor por defecto", ct);
    }

    public async Task<PromptAgente> RevertirAsync(
        string agenteKey, int version, int? usuarioId, CancellationToken ct = default)
    {
        ValidarKey(agenteKey);
        var origen = await _db.PromptsAgente
            .FirstOrDefaultAsync(p => p.AgenteKey == agenteKey && p.Version == version, ct)
            ?? throw new ArgumentException($"No existe la versión {version} para '{agenteKey}'.");

        return await GuardarNuevaVersionAsync(
            agenteKey, origen.Contenido, usuarioId, $"Revertido a la versión {version}", ct);
    }
}
