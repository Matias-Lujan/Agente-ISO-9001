// ============================================================================
//  AuditoriaEndpoints — Endpoints REST reales del workflow (D6)
// ----------------------------------------------------------------------------
//  Dos endpoints mínimos del MVP:
//
//   POST /api/auditorias       Crea auditoría EnCurso, encola, responde 202.
//   GET  /api/auditorias/{id}  Devuelve metadata + estado (para polling).
//
//  ALCANCE D6:
//   - Sin autenticación / JWT.
//   - Sin autorización por roles.
//   - GET devuelve SOLO metadata (sin contadores ni listas).
//   - Otros endpoints del ERS (/proyectos, /hallazgos, /informes, /usuarios)
//     son D6+ o post-MVP.
//
//  TEMPORAL — Autenticación:
//   El ERS pide autenticación con JWT. En D6 el UsuarioId llega por body.
//   Cuando se agregue JWT (etapa posterior), UsuarioId sale del User claim
//   y el campo del body se elimina. Dejo esta nota explícita acá para que
//   quien retome el código sepa que el body actual es transitorio.
//
//  NO AGREGAR ACÁ:
//   - Validación FK previa contra BD (la hace MySQL al commit).
//   - Atomicidad commit+encolar (el Channel write no puede fallar salvo
//     que el host se apague, riesgo aceptado).
//   - Mapeo de hallazgos / informes (eso requiere otros repos / endpoints).
// ============================================================================

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
            IAuditoriaRepository auditoriaRepo,
            IUnitOfWork uow,
            IAuditoriaQueue cola,
            CancellationToken ct) =>
        {
            // 1. Validación de input — los 3 IDs deben ser > 0.
            if (request.ProyectoId <= 0)
                return Results.BadRequest("proyectoId debe ser > 0.");
            if (request.EtapaId <= 0)
                return Results.BadRequest("etapaId debe ser > 0.");
            if (request.UsuarioId <= 0)
                return Results.BadRequest("usuarioId debe ser > 0.");

            int auditoriaId = 0;

            try
            {
                await uow.EjecutarEnTransaccionAsync(async innerCt =>
                {
                    auditoriaId = await auditoriaRepo.CrearEnCursoAsync(
                        request.ProyectoId,
                        request.UsuarioId,
                        request.EtapaId,
                        innerCt);
                }, ct);
            }
            catch (DbUpdateException ex)
            {
                return Results.BadRequest(
                    "No se pudo crear la auditoría. Verificá que proyectoId, " +
                    $"etapaId y usuarioId existan en la BD. Detalle: {ex.InnerException?.Message ?? ex.Message}");
            }

            // 4. Encolar (DESPUÉS del commit, para no encolar IDs no persistidos).
            //    El Channel write es ilimitado — no falla salvo apagado del host.
            await cola.EncolarAsync(auditoriaId, ct);

            // 5. 202 Accepted con el ID + estado inicial.
            return Results.Accepted(
                uri: $"/api/auditorias/{auditoriaId}",
                value: new
                {
                    auditoriaId,
                    estado = "EnCurso"
                });
        });

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
                return Results.NotFound($"Auditoría {id} no encontrada.");

            // DTO de respuesta — no exponemos la entity EF directamente.
            // Las navegaciones (Proyecto, Usuario, Etapa, ArtefactosEvaluados,
            // etc.) NO se serializan: en D6 solo devolvemos metadata.
            return Results.Ok(new
            {
                id = auditoria.Id,
                proyectoId = auditoria.ProyectoId,
                etapaId = auditoria.EtapaId,
                usuarioId = auditoria.UsuarioId,
                estado = auditoria.Estado.ToString(),  // "EnCurso" / "Completada" / "Fallida"
                fechaInicioUtc = auditoria.FechaInicioUtc,
                fechaFinalizacionUtc = auditoria.FechaFinalizacionUtc
            });
        });

        return app;
    }
}

/// <summary>
/// Request body del POST /api/auditorias.
/// usuarioId es TEMPORAL — sale del JWT cuando se agregue autenticación.
/// </summary>
public sealed record CrearAuditoriaRequest(
    int ProyectoId,
    int EtapaId,
    int UsuarioId);