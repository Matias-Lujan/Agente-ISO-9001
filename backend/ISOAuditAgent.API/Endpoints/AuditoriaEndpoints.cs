// ============================================================================
//  AuditoriaEndpoints — Endpoints REST reales del workflow (D6)
// ----------------------------------------------------------------------------
//  Endpoints del MVP:
//
//   POST /api/auditorias                Crea auditoria EnCurso, encola, 202.
//   GET  /api/auditorias/{id}           Devuelve metadata + estado (polling).
//   GET  /api/auditorias/{id}/progreso  Progreso por nodo del workflow.
//
//  AUTENTICACION:
//   - Todos los endpoints requieren JWT valido (RequireAuthorization()).
//   - El UsuarioId del POST sale del claim NameIdentifier del JWT.
//     Eso significa que NO viene mas en el body: el usuario "que crea" la
//     auditoria es siempre el que esta logueado.
// ============================================================================

using System.Security.Claims;
using ISOAuditAgent.API.Agents.Orchestrator;
using ISOAuditAgent.API.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Endpoints;

public static class AuditoriaEndpoints
{
    public static WebApplication MapAuditoriaEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        // --- POST /api/auditorias --------------------------------------------
        app.MapPost("/api/auditorias", async (
            CrearAuditoriaRequest request,
            ClaimsPrincipal user,
            IAuditoriaRepository auditoriaRepo,
            IUnitOfWork uow,
            IAuditoriaQueue cola,
            CancellationToken ct) =>
        {
            // 1. Validacion de input — los 2 IDs deben ser > 0.
            //    usuarioId ya NO esta en el body: sale del JWT.
            if (request.ProyectoId <= 0)
                return Results.BadRequest("proyectoId debe ser > 0.");
            if (request.EtapaId <= 0)
                return Results.BadRequest("etapaId debe ser > 0.");

            // 2. Extraer usuarioId del JWT (claim NameIdentifier).
            //    Si llegamos aca es porque [RequireAuthorization] ya valido
            //    el token. Pero el claim podria faltar si alguien firma un
            //    JWT sin NameIdentifier (no nuestro caso, pero defensa).
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(idClaim, out var usuarioId) || usuarioId <= 0)
                return Results.Unauthorized();

            int auditoriaId = 0;

            try
            {
                await uow.EjecutarEnTransaccionAsync(async innerCt =>
                {
                    auditoriaId = await auditoriaRepo.CrearEnCursoAsync(
                        request.ProyectoId,
                        usuarioId,
                        request.EtapaId,
                        innerCt);
                }, ct);
            }
            catch (DbUpdateException ex)
            {
                return Results.BadRequest(
                    "No se pudo crear la auditoria. Verifica que proyectoId y " +
                    $"etapaId existan en la BD. Detalle: {ex.InnerException?.Message ?? ex.Message}");
            }

            // 3. Encolar (DESPUES del commit, para no encolar IDs no persistidos).
            await cola.EncolarAsync(auditoriaId, ct);

            // 4. 202 Accepted con el ID + estado inicial.
            return Results.Accepted(
                uri: $"/api/auditorias/{auditoriaId}",
                value: new
                {
                    auditoriaId,
                    estado = "EnCurso"
                });
        })
        .RequireAuthorization();

        // --- GET /api/auditorias/{id} ----------------------------------------
        app.MapGet("/api/auditorias/{id:int}", async (
            int id,
            IAuditoriaRepository auditoriaRepo,
            CancellationToken ct) =>
        {
            if (id <= 0)
                return Results.BadRequest("id debe ser > 0.");

            var auditoria = await auditoriaRepo.ObtenerPorIdOrNullAsync(id, ct);
            if (auditoria is null)
                return Results.NotFound($"Auditoria {id} no encontrada.");

            return Results.Ok(new
            {
                id = auditoria.Id,
                proyectoId = auditoria.ProyectoId,
                etapaId = auditoria.EtapaId,
                usuarioId = auditoria.UsuarioId,
                estado = auditoria.Estado.ToString(),
                fechaInicioUtc = auditoria.FechaInicioUtc,
                fechaFinalizacionUtc = auditoria.FechaFinalizacionUtc
            });
        })
        .RequireAuthorization();

        // --- GET /api/auditorias/{id}/progreso -------------------------------
        app.MapGet("/api/auditorias/{id:int}/progreso", async (
            int id,
            IAuditoriaProgresoRepository progresoRepo,
            CancellationToken ct) =>
        {
            if (id <= 0)
                return Results.BadRequest("id debe ser > 0.");

            var filas = await progresoRepo.ObtenerPorAuditoriaAsync(id, ct);

            return Results.Ok(filas.Select(p => new
            {
                nodo = p.Nodo.ToString(),
                estado = p.Estado.ToString(),
                fechaInicioUtc = p.FechaInicioUtc,
                fechaFinUtc = p.FechaFinUtc
            }));
        })
        .RequireAuthorization();

        return app;
    }
}

/// <summary>
/// Request body del POST /api/auditorias.
/// usuarioId ya no viene en el body — sale del JWT del usuario logueado.
/// </summary>
public sealed record CrearAuditoriaRequest(
    int ProyectoId,
    int EtapaId);