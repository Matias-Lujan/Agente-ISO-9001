namespace ISOAuditAgent.API.DTOs;

// ============================================================================
//  DTOs de analítica (dashboard + revisión).
//
//  Todo se calcula en el backend (AnalyticsService) con una sola regla:
//   - Solo cuentan auditorías Completada y artefactos con Aplica == Aplica.
//   - "actual"    = última Completada por proyecto.
//   - "histórico" = todas las Completada.
//  Así el dashboard y la pantalla de Hallazgos no pueden discrepar.
//
//  Los nombres de propiedad se serializan en camelCase (default de ASP.NET),
//  por eso espejan los campos que ya consume el frontend.
// ============================================================================

public record DashboardMetricasResponse(
    int ProyectosAuditados,
    int ProyectosTotal,
    int NoConformidades,
    int Observaciones,
    int OportunidadesMejora,
    int TotalHallazgos,
    int AuditoriasCompletadas,
    int AuditoriasTotal);

public record HallazgosPorTipoResponse(int Nc, int Obs, int Om);

public record ProyectosPorEstadoResponse(int Completada, int EnCurso, int Fallida, int SinAuditar);

public record CumplimientoGeneralResponse(int Revisados, int Conformes, int NoConformes, int Pct);

public record ConteoAgenteResponse(string Agente, int Cantidad);

public record CumplProcedimientoResponse(string Procedimiento, int Pct);

/// <summary>Un punto de la curva de evolución (un día con hallazgos). Base: histórico.</summary>
public record SerieEvolucionResponse(string Etiqueta, int Valor, int Orden, string Fecha);

public record ProyectoCumplResponse(
    int Id,
    string Nombre,
    string ProcCodigo,
    int? Cumpl,                 // % de la última auditoría completada (null si no tiene)
    int Nc,                     // NC de esa ejecución
    string EstadoUltima,        // estado de la última auditoría (cualquier estado) | "SinAuditar"
    DateTime? Fecha);

public record UltimoHallazgoResponse(
    int Id,
    string Descripcion,
    string Proyecto,
    string Tipo,                // "NC" | "OBS" | "OM"
    DateTime? Fecha);

/// <summary>Respuesta agregada del dashboard. KPIs/donuts/atención = actual; evolución = histórico.</summary>
public record DashboardResponse(
    DashboardMetricasResponse Metricas,
    IReadOnlyList<UltimoHallazgoResponse> UltimosHallazgos,
    HallazgosPorTipoResponse HallazgosPorTipo,
    ProyectosPorEstadoResponse ProyectosPorEstado,
    CumplimientoGeneralResponse CumplimientoGeneral,
    IReadOnlyList<ProyectoCumplResponse> CumplPorProyecto,
    IReadOnlyList<CumplProcedimientoResponse> CumplPorProcedimiento,
    IReadOnlyList<ConteoAgenteResponse> HallazgosPorAgente,
    IReadOnlyList<SerieEvolucionResponse> Evolucion,
    IReadOnlyList<ProyectoCumplResponse> Atencion);

// ── Revisión (pantalla Hallazgos) ────────────────────────────────────────────

public record RevisionHallazgoResponse(
    int Id,
    string Titulo,              // nombre del artefacto evaluado
    string Descripcion,
    string Evidencia,          // justificación del agente
    string Proyecto,
    string Tipo,                // "NC" | "OBS" | "OM"
    DateTime Fecha,             // fecha de inicio de la auditoría que lo originó
    string AgenteOrigen);

public record RevisionConformeResponse(
    int Id,
    string? Codigo,
    string Nombre,
    string Proyecto);

public record RevisionResponse(
    IReadOnlyList<RevisionHallazgoResponse> Hallazgos,
    IReadOnlyList<RevisionConformeResponse> Conformes,
    int Revisados,
    int TotalConformes,
    int TotalNoConformes);
