// ============================================================================
//  MetadataEndpoints — Endpoints de lectura para el frontend
// ----------------------------------------------------------------------------
//  Endpoints que el frontend usa para poblar selectores de "Nueva auditoria":
//
//   GET /api/proyectos                       Lista de proyectos activos.
//   GET /api/procedimientos/{id}/etapas      Etapas del procedimiento dado.
//
//  AUTENTICACION:
//   Ambos endpoints requieren JWT — no exponemos la lista de proyectos a
//   visitantes anonimos.
// ============================================================================

using ISOAuditAgent.API.Repositories;

namespace ISOAuditAgent.API.Endpoints;

public static class MetadataEndpoints
{
    public static WebApplication MapMetadataEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // --- GET /api/proyectos ----------------------------------------------
        app.MapGet("/api/proyectos", async (
            IProyectoRepository repo,
            CancellationToken ct) =>
        {
            var proyectos = await repo.ObtenerActivosAsync(ct);

            return Results.Ok(proyectos.Select(p => new
            {
                id = p.Id,
                nombre = p.Nombre,
                procedimientoId = p.ProcedimientoId
            }));
        })
        .RequireAuthorization();

        // --- GET /api/procedimientos/{id}/etapas -----------------------------
        app.MapGet("/api/procedimientos/{procedimientoId:int}/etapas", async (
            int procedimientoId,
            IProcedimientoRepository repo,
            CancellationToken ct) =>
        {
            if (procedimientoId <= 0)
                return Results.BadRequest("procedimientoId debe ser > 0.");

            var etapas = await repo.ObtenerEtapasAsync(procedimientoId, ct);

            return Results.Ok(etapas.Select(e => new
            {
                id = e.Id,
                nombre = e.Nombre,
                orden = e.Orden
            }))
            ;
        })
        .RequireAuthorization();

        return app;
    }
}