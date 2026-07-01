using System.Globalization;
using ISOAuditAgent.API.Data;
using ISOAuditAgent.API.DTOs;
using ISOAuditAgent.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ISOAuditAgent.API.Services;

/// <summary>Alcance de las métricas de hallazgos.</summary>
public enum AlcanceAnalitica
{
    /// <summary>Última auditoría Completada de cada proyecto (estado actual).</summary>
    Actual,
    /// <summary>Todas las auditorías Completada (acumulado en el tiempo).</summary>
    Historico,
}

/// <summary>
/// Única fuente de verdad para la analítica de hallazgos y cumplimiento.
///
/// Regla canónica (la misma para dashboard y para la pantalla de Hallazgos, así
/// los números no pueden discrepar entre pantallas):
///   - Solo cuentan auditorías Completada y artefactos con Aplica == Aplica.
///   - "actual"     = última Completada por proyecto.
///   - "histórico"  = todas las Completada.
///   - Cumplimiento = Conforme / (Conforme + NoConforme) entre aplicables.
///
/// El dashboard usa KPIs/donuts/atención en base "actual" y la evolución en base
/// "histórico"; la pantalla de Hallazgos elige el alcance con un parámetro.
/// </summary>
public class AnalyticsService
{
    private readonly ISOAuditAgentDbContext _db;

    public AnalyticsService(ISOAuditAgentDbContext db) => _db = db;

    // ── Carga base ──────────────────────────────────────────────────────────────

    private sealed record Datos(
        List<Proyecto> Proyectos,
        List<Auditoria> Auditorias,    // todas (cualquier estado), sin detalle
        List<Auditoria> Completadas);  // con artefactos + hallazgos

    /// <param name="proyectoIds">null = todos los proyectos activos; si viene, se
    /// limita a esos ids (scope por rol: el Operador ve solo los suyos).</param>
    private async Task<Datos> CargarAsync(IReadOnlyList<int>? proyectoIds, CancellationToken ct)
    {
        var proyectosQ = _db.Proyectos.Where(p => p.Activo);
        if (proyectoIds is not null)
            proyectosQ = proyectosQ.Where(p => proyectoIds.Contains(p.Id));

        var proyectos = await proyectosQ
            .Include(p => p.Procedimiento)
            .AsNoTracking()
            .ToListAsync(ct);

        var ids = proyectos.Select(p => p.Id).ToList();

        var auditorias = await _db.Auditorias
            .Where(a => ids.Contains(a.ProyectoId))
            .AsNoTracking()
            .ToListAsync(ct);

        var completadas = await _db.Auditorias
            .Where(a => ids.Contains(a.ProyectoId) && a.Estado == EstadoAuditoria.Completada)
            .Include(a => a.ArtefactosEvaluados).ThenInclude(ae => ae.ArtefactoEsperado)
            .Include(a => a.ArtefactosEvaluados).ThenInclude(ae => ae.Hallazgos)
            .AsNoTracking()
            .ToListAsync(ct);

        return new Datos(proyectos, auditorias, completadas);
    }

    // ── Helpers de la regla canónica ────────────────────────────────────────────

    private static IEnumerable<ArtefactoEvaluado> Aplicables(Auditoria a) =>
        a.ArtefactosEvaluados.Where(ae => ae.Aplica == EstadoAplicacionTailoring.Aplica);

    private static IEnumerable<Hallazgo> HallazgosDe(Auditoria a) =>
        Aplicables(a).SelectMany(ae => ae.Hallazgos);

    // Redondeo "half away from zero" para igualar el Math.round() de JS.
    private static int Pct(double parte, double total) =>
        (int)Math.Round(parte / total * 100, MidpointRounding.AwayFromZero);

    // ── Dashboard ────────────────────────────────────────────────────────────────

    public async Task<DashboardResponse> ObtenerDashboardAsync(
        IReadOnlyList<int>? proyectoIds, CancellationToken ct)
    {
        var d = await CargarAsync(proyectoIds, ct);

        var ultimaCompletada = d.Completadas
            .GroupBy(a => a.ProyectoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.FechaInicioUtc).First());

        var ultimaCualquiera = d.Auditorias
            .GroupBy(a => a.ProyectoId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(a => a.FechaInicioUtc).First());

        // Hallazgos "actual": de la última Completada de cada proyecto.
        var hallazgosActual = ultimaCompletada.Values.SelectMany(HallazgosDe).ToList();
        int nc  = hallazgosActual.Count(h => h.Tipo == TipoHallazgo.NC);
        int obs = hallazgosActual.Count(h => h.Tipo == TipoHallazgo.OBS);
        int om  = hallazgosActual.Count(h => h.Tipo == TipoHallazgo.OM);

        // Cumplimiento por proyecto (actual) + acumulado general.
        int gConf = 0, gNoConf = 0;
        var filas = new List<ProyectoCumplResponse>();
        foreach (var p in d.Proyectos)
        {
            ultimaCompletada.TryGetValue(p.Id, out var uc);
            ultimaCualquiera.TryGetValue(p.Id, out var ua);

            int? cumpl = null;
            int filaNc = 0;
            if (uc is not null)
            {
                var aplic = Aplicables(uc).ToList();
                int conf   = aplic.Count(ae => ae.Resultado == ResultadoEvaluacion.Conforme);
                int noConf = aplic.Count(ae => ae.Resultado == ResultadoEvaluacion.NoConforme);
                gConf += conf; gNoConf += noConf;
                if (conf + noConf > 0) cumpl = Pct(conf, conf + noConf);
                filaNc = aplic.SelectMany(ae => ae.Hallazgos).Count(h => h.Tipo == TipoHallazgo.NC);
            }

            filas.Add(new ProyectoCumplResponse(
                p.Id, p.Nombre,
                p.Procedimiento?.Codigo ?? $"Proc #{p.ProcedimientoId}",
                cumpl, filaNc,
                ua?.Estado.ToString() ?? "SinAuditar",
                ua?.FechaInicioUtc));
        }

        // Proyectos por estado (según la última auditoría de cualquier estado).
        int cCompletada = 0, cEnCurso = 0, cFallida = 0, cSin = 0;
        foreach (var p in d.Proyectos)
        {
            if (!ultimaCualquiera.TryGetValue(p.Id, out var ua)) { cSin++; continue; }
            switch (ua.Estado)
            {
                case EstadoAuditoria.Completada: cCompletada++; break;
                case EstadoAuditoria.EnCurso:    cEnCurso++;    break;
                case EstadoAuditoria.Fallida:    cFallida++;    break;
                default:                          cSin++;        break;
            }
        }

        // Cumplimiento por procedimiento (promedio de proyectos con cumplimiento).
        var cumplPorProc = filas
            .Where(f => f.Cumpl.HasValue)
            .GroupBy(f => f.ProcCodigo)
            .Select(g => new CumplProcedimientoResponse(
                g.Key,
                (int)Math.Round(g.Average(f => f.Cumpl!.Value), MidpointRounding.AwayFromZero)))
            .OrderByDescending(c => c.Pct)
            .ToList();

        // Hallazgos por agente (actual).
        var porAgente = hallazgosActual
            .GroupBy(h => h.AgenteOrigen.ToString())
            .Select(g => new ConteoAgenteResponse(g.Key, g.Count()))
            .OrderByDescending(c => c.Cantidad)
            .ToList();

        // Evolución (HISTÓRICO): hallazgos aplicables por día, sobre todas las Completada.
        var porDia = new Dictionary<string, int>();
        foreach (var a in d.Completadas)
        {
            int cant = HallazgosDe(a).Count();
            if (cant == 0) continue;
            var key = a.FechaInicioUtc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
            porDia[key] = porDia.GetValueOrDefault(key) + cant;
        }
        var evolucion = porDia
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select((kv, i) =>
            {
                var fecha = DateTime.ParseExact(kv.Key, "yyyy-MM-dd", CultureInfo.InvariantCulture);
                return new SerieEvolucionResponse(
                    fecha.ToString("dd/MM", CultureInfo.InvariantCulture), kv.Value, i, kv.Key);
            })
            .ToList();

        // Últimos hallazgos (recencia, sobre todas las Completada).
        var nombrePorProy = d.Proyectos.ToDictionary(p => p.Id, p => p.Nombre);
        var ultimos = d.Completadas
            .SelectMany(a => HallazgosDe(a).Select(h => new
            {
                h.Id, h.Descripcion, h.Tipo,
                Proyecto = nombrePorProy.GetValueOrDefault(a.ProyectoId, "—"),
                Fecha = (DateTime?)a.FechaInicioUtc,
            }))
            .OrderByDescending(x => x.Fecha).ThenByDescending(x => x.Id)
            .Take(6)
            .Select(x => new UltimoHallazgoResponse(
                x.Id, x.Descripcion, x.Proyecto, x.Tipo.ToString(), x.Fecha))
            .ToList();

        // Atención: cumplimiento bajo (< 70) o con NC.
        var atencion = filas
            .Where(f => (f.Cumpl.HasValue && f.Cumpl < 70) || f.Nc > 0)
            .OrderBy(f => f.Cumpl ?? 999)
            .ToList();

        int revisadosG = gConf + gNoConf;
        var metricas = new DashboardMetricasResponse(
            ProyectosAuditados:    d.Auditorias.Select(a => a.ProyectoId).Distinct().Count(),
            ProyectosTotal:        d.Proyectos.Count,
            NoConformidades:       nc,
            Observaciones:         obs,
            OportunidadesMejora:   om,
            TotalHallazgos:        hallazgosActual.Count,
            AuditoriasCompletadas: d.Auditorias.Count(a => a.Estado == EstadoAuditoria.Completada),
            AuditoriasTotal:       d.Auditorias.Count);

        return new DashboardResponse(
            metricas,
            ultimos,
            new HallazgosPorTipoResponse(nc, obs, om),
            new ProyectosPorEstadoResponse(cCompletada, cEnCurso, cFallida, cSin),
            new CumplimientoGeneralResponse(
                revisadosG, gConf, gNoConf, revisadosG > 0 ? Pct(gConf, revisadosG) : 0),
            filas.OrderByDescending(f => f.Cumpl ?? -1).ToList(),
            cumplPorProc,
            porAgente,
            evolucion,
            atencion);
    }

    // ── Revisión (pantalla Hallazgos) ────────────────────────────────────────────

    public async Task<RevisionResponse> ObtenerRevisionAsync(
        AlcanceAnalitica scope, IReadOnlyList<int>? proyectoIds, CancellationToken ct)
    {
        var d = await CargarAsync(proyectoIds, ct);
        var nombrePorProy = d.Proyectos.ToDictionary(p => p.Id, p => p.Nombre);

        // Conjunto de auditorías según el alcance pedido.
        IEnumerable<Auditoria> auds = scope == AlcanceAnalitica.Historico
            ? d.Completadas
            : d.Completadas
                .GroupBy(a => a.ProyectoId)
                .Select(g => g.OrderByDescending(a => a.FechaInicioUtc).First());

        var hallazgos = new List<RevisionHallazgoResponse>();
        var conformes = new List<RevisionConformeResponse>();
        int totalConf = 0, totalNoConf = 0;

        foreach (var a in auds)
        {
            var proyNombre = nombrePorProy.GetValueOrDefault(a.ProyectoId, "—");
            foreach (var ae in Aplicables(a))
            {
                var nombreArt = ae.ArtefactoEsperado?.Nombre ?? $"Artefacto #{ae.ArtefactoEsperadoId}";

                if (ae.Resultado == ResultadoEvaluacion.Conforme)
                {
                    totalConf++;
                    conformes.Add(new RevisionConformeResponse(
                        ae.Id, ae.ArtefactoEsperado?.Codigo, nombreArt, proyNombre));
                }
                else if (ae.Resultado == ResultadoEvaluacion.NoConforme)
                {
                    totalNoConf++;
                }

                foreach (var h in ae.Hallazgos)
                {
                    hallazgos.Add(new RevisionHallazgoResponse(
                        h.Id, nombreArt, h.Descripcion, h.Justificacion ?? "",
                        proyNombre, h.Tipo.ToString(), a.FechaInicioUtc, h.AgenteOrigen.ToString()));
                }
            }
        }

        var ordenados = hallazgos
            .OrderByDescending(h => h.Fecha).ThenByDescending(h => h.Id)
            .ToList();

        return new RevisionResponse(ordenados, conformes, totalConf + totalNoConf, totalConf, totalNoConf);
    }
}
