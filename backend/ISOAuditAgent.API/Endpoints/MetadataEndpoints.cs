// ============================================================================
//  MetadataEndpoints — Endpoints de lectura para el frontend
// ----------------------------------------------------------------------------
//  Endpoints chiquitos que el frontend necesita para poblar los selectores de
//  la pantalla "Nueva auditoría":
//
//   GET /api/proyectos                       Lista de proyectos activos.
//   GET /api/procedimientos/{id}/etapas      Etapas del procedimiento dado.
//
//  El cliente pide proyectos primero, y al elegir uno hace el segundo request
//  con procedimientoId del proyecto seleccionado. Esto mantiene la coherencia
//  del modelo de datos: una Etapa pertenece a un Procedimiento, no es global.
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
        });

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
            }));
        });

        return app;
    }
}
