// ============================================================================
//  Módulo del dashboard del auditor.
//
//  No existe (todavía) un endpoint agregado tipo GET /api/dashboard, así que
//  esta capa arma el resumen combinando los endpoints reales que SÍ existen:
//
//    1. GET /api/proyectos                         → proyectos visibles al usuario
//    2. GET /api/auditorias/proyecto/{proyectoId}  → auditorías de cada proyecto
//    3. GET /api/hallazgos/auditoria/{auditoriaId} → hallazgos de cada auditoría
//
//  Funciona para cualquier rol autenticado (no requiere ser Administrador).
//  Si en el futuro el backend expone un endpoint agregado, solo se reemplaza
//  cargarDashboard() y la pantalla sigue igual.
//
//  Limitaciones del modelo actual (no son mock, simplemente el backend no lo da):
//   - El hallazgo no tiene fecha propia → usamos la fecha de inicio de su auditoría.
//   - No hay métrica de "compliance %" → se reemplazó por "auditorías completadas".
// ============================================================================

import { api } from './client';
import { listarProyectos } from './proyectos';
import { listarProcedimientos } from './procedimientos';
import {
  listarAuditoriasDeProyecto,
  obtenerResultado,
  type AuditoriaDeProyecto,
} from './auditorias';

// ── Shapes tal cual los devuelve el backend ──────────────────────────────────

type EstadoAuditoria = 'EnCurso' | 'Completada' | 'Fallida';

// Espejo de AuditoriaResponse (backend DTOs/AuditoriaDTOs.cs)
interface AuditoriaApi {
  id: number;
  proyectoId: number;
  nombreProyecto: string;
  usuarioId: number;
  nombreAuditor: string;
  etapaId: number;
  fechaInicioUtc: string;
  fechaFinalizacionUtc: string | null;
  estado: EstadoAuditoria;
}

// Espejo de HallazgoResponse (backend DTOs/HallazgoDTOs.cs)
type TipoHallazgo = 'NC' | 'OBS' | 'OM';

interface HallazgoApi {
  id: number;
  auditoriaId: number;
  artefactoEvaluadoId: number;
  nombreArtefacto: string;
  tipo: TipoHallazgo;
  descripcion: string;
  justificacion: string;
  agenteOrigen: string;
}

// ── Shape que consume la pantalla ────────────────────────────────────────────

export type { EstadoAuditoria, TipoHallazgo };

export interface DashboardMetricas {
  proyectosAuditados: number;
  proyectosTotal: number;
  noConformidades: number;
  observaciones: number;
  oportunidadesMejora: number;
  totalHallazgos: number;
  auditoriasCompletadas: number;
  auditoriasTotal: number;
}

export interface ProyectoReciente {
  auditoriaId: number;
  proyecto: string;
  fecha: string; // ISO de la fecha de inicio de la auditoría
  estado: EstadoAuditoria;
  hallazgosNC: number;
  hallazgosTotal: number;
}

export interface UltimoHallazgo {
  id: number;
  descripcion: string;
  proyecto: string;
  tipo: TipoHallazgo;
  fecha: string; // ISO — fecha de la auditoría que lo originó
}

export interface DashboardData {
  metricas: DashboardMetricas;
  proyectosRecientes: ProyectoReciente[];
  ultimosHallazgos: UltimoHallazgo[];
}

const MAX_PROYECTOS_RECIENTES = 5;
const MAX_ULTIMOS_HALLAZGOS = 6;

// ── Carga + agregación ───────────────────────────────────────────────────────

/**
 * Arma el resumen del dashboard a partir de los endpoints reales.
 * Si una llamada anidada falla (p. ej. un proyecto sin auditorías), se trata
 * como vacía en vez de romper todo el dashboard.
 */
export async function cargarDashboard(): Promise<DashboardData> {
  const proyectos = await listarProyectos();

  // Auditorías de cada proyecto, en paralelo.
  const auditoriasAnidadas = await Promise.all(
    proyectos.map((p) =>
      api.get<AuditoriaApi[]>(`/api/auditorias/proyecto/${p.id}`).catch(() => [])
    )
  );
  const auditorias = auditoriasAnidadas.flat();

  // Hallazgos de cada auditoría, en paralelo.
  const hallazgosAnidados = await Promise.all(
    auditorias.map((a) =>
      api.get<HallazgoApi[]>(`/api/hallazgos/auditoria/${a.id}`).catch(() => [])
    )
  );
  const hallazgos = hallazgosAnidados.flat();

  // Índice auditoríaId → auditoría, para mapear proyecto/fecha de los hallazgos.
  const auditoriaPorId = new Map<number, AuditoriaApi>();
  for (const a of auditorias) auditoriaPorId.set(a.id, a);

  return {
    metricas: calcularMetricas(proyectos.length, auditorias, hallazgos),
    proyectosRecientes: calcularProyectosRecientes(auditorias, hallazgos),
    ultimosHallazgos: calcularUltimosHallazgos(hallazgos, auditoriaPorId),
  };
}

function contarPorTipo(hallazgos: HallazgoApi[], tipo: TipoHallazgo): number {
  return hallazgos.filter((h) => h.tipo === tipo).length;
}

function calcularMetricas(
  proyectosTotal: number,
  auditorias: AuditoriaApi[],
  hallazgos: HallazgoApi[]
): DashboardMetricas {
  const proyectosAuditados = new Set(auditorias.map((a) => a.proyectoId)).size;

  return {
    proyectosAuditados,
    proyectosTotal,
    noConformidades: contarPorTipo(hallazgos, 'NC'),
    observaciones: contarPorTipo(hallazgos, 'OBS'),
    oportunidadesMejora: contarPorTipo(hallazgos, 'OM'),
    totalHallazgos: hallazgos.length,
    auditoriasCompletadas: auditorias.filter((a) => a.estado === 'Completada').length,
    auditoriasTotal: auditorias.length,
  };
}

function calcularProyectosRecientes(
  auditorias: AuditoriaApi[],
  hallazgos: HallazgoApi[]
): ProyectoReciente[] {
  // Hallazgos por auditoría (para contar NC y total por fila).
  const totalPorAuditoria = new Map<number, number>();
  const ncPorAuditoria = new Map<number, number>();
  for (const h of hallazgos) {
    totalPorAuditoria.set(h.auditoriaId, (totalPorAuditoria.get(h.auditoriaId) ?? 0) + 1);
    if (h.tipo === 'NC') {
      ncPorAuditoria.set(h.auditoriaId, (ncPorAuditoria.get(h.auditoriaId) ?? 0) + 1);
    }
  }

  // Auditoría más reciente por proyecto.
  const masRecientePorProyecto = new Map<number, AuditoriaApi>();
  for (const a of auditorias) {
    const actual = masRecientePorProyecto.get(a.proyectoId);
    if (!actual || fechaMs(a.fechaInicioUtc) > fechaMs(actual.fechaInicioUtc)) {
      masRecientePorProyecto.set(a.proyectoId, a);
    }
  }

  return [...masRecientePorProyecto.values()]
    .sort((a, b) => fechaMs(b.fechaInicioUtc) - fechaMs(a.fechaInicioUtc))
    .slice(0, MAX_PROYECTOS_RECIENTES)
    .map((a) => ({
      auditoriaId: a.id,
      proyecto: a.nombreProyecto,
      fecha: a.fechaInicioUtc,
      estado: a.estado,
      hallazgosNC: ncPorAuditoria.get(a.id) ?? 0,
      hallazgosTotal: totalPorAuditoria.get(a.id) ?? 0,
    }));
}

function calcularUltimosHallazgos(
  hallazgos: HallazgoApi[],
  auditoriaPorId: Map<number, AuditoriaApi>
): UltimoHallazgo[] {
  return hallazgos
    .map((h) => {
      const auditoria = auditoriaPorId.get(h.auditoriaId);
      return {
        id: h.id,
        descripcion: h.descripcion,
        proyecto: auditoria?.nombreProyecto ?? '—',
        tipo: h.tipo,
        fecha: auditoria?.fechaInicioUtc ?? '',
      };
    })
    // Más recientes primero: por fecha de auditoría y, a igualdad, por id.
    .sort((a, b) => fechaMs(b.fecha) - fechaMs(a.fecha) || b.id - a.id)
    .slice(0, MAX_ULTIMOS_HALLAZGOS);
}

function fechaMs(iso: string): number {
  const ms = Date.parse(iso);
  return Number.isNaN(ms) ? 0 : ms;
}

// ============================================================================
//  cargarDashboardCompleto() — versión enriquecida para el dashboard nuevo.
//
//  Agrega, sobre la data anterior, todo lo que necesitan los gráficos:
//   - hallazgos por tipo / por agente / evolución por mes
//   - proyectos por estado (de su última auditoría)
//   - cumplimiento general + por proyecto + por procedimiento  (vía resultado)
//   - proyectos que requieren atención (cumplimiento < 70% o con NC)
//
//  Todo se deriva de endpoints reales; si una llamada anidada falla, se trata
//  como vacía y el resto del dashboard sigue funcionando.
// ============================================================================

export interface ProyectoCumpl {
  id: number;
  nombre: string;
  procCodigo: string;
  cumpl: number | null;                         // % de la última auditoría completada
  nc: number;                                   // NC de esa ejecución
  estadoUltima: EstadoAuditoria | 'SinAuditar';
  fecha: string | null;
}
export interface SerieEvolucion { etiqueta: string; valor: number; orden: number; fecha: string; }
export interface ConteoAgente { agente: string; cantidad: number; }
export interface CumplProc { procedimiento: string; pct: number; }
export interface ProyectosPorEstado {
  completada: number; enCurso: number; fallida: number; sinAuditar: number;
}

export interface DashboardCompleto {
  metricas: DashboardMetricas;
  ultimosHallazgos: UltimoHallazgo[];
  hallazgosPorTipo: { nc: number; obs: number; om: number };
  proyectosPorEstado: ProyectosPorEstado;
  cumplimientoGeneral: { revisados: number; conformes: number; noConformes: number; pct: number };
  cumplPorProyecto: ProyectoCumpl[];
  cumplPorProcedimiento: CumplProc[];
  hallazgosPorAgente: ConteoAgente[];
  evolucion: SerieEvolucion[];
  atencion: ProyectoCumpl[];
}

// El resultado de auditoría puede serializar el tipo como 'NC' o como el nombre
// del enum ('NoConformidad'…): aceptamos ambos.
function mapTipoFlexible(t: string): TipoHallazgo {
  const x = (t ?? '').toLowerCase();
  if (x === 'nc' || x.includes('conformidad')) return 'NC';
  if (x === 'obs' || x.includes('observ')) return 'OBS';
  return 'OM';
}

export async function cargarDashboardCompleto(): Promise<DashboardCompleto> {
  const [proyectos, procedimientos] = await Promise.all([
    listarProyectos(),
    listarProcedimientos().catch(() => []),
  ]);
  const procCodigo = new Map<number, string>();
  for (const pr of procedimientos) procCodigo.set(pr.id, pr.codigo);

  // Auditorías de cada proyecto.
  const auditoriasPorProyecto = await Promise.all(
    proyectos.map((p) =>
      listarAuditoriasDeProyecto(p.id).catch(() => [] as AuditoriaDeProyecto[])
    )
  );
  const auditorias = auditoriasPorProyecto.flat();
  const auditoriaPorId = new Map<number, AuditoriaDeProyecto>();
  for (const a of auditorias) auditoriaPorId.set(a.id, a);

  // Hallazgos de TODAS las auditorías (tipo + agente + fecha).
  const hallazgosAnidados = await Promise.all(
    auditorias.map((a) =>
      api.get<HallazgoApi[]>(`/api/hallazgos/auditoria/${a.id}`).catch(() => [])
    )
  );
  const hallazgos = hallazgosAnidados.flat();

  // Resultado de la última Completada por proyecto → cumplimiento.
  let gConf = 0, gNoConf = 0;
  const filas: ProyectoCumpl[] = await Promise.all(
    proyectos.map(async (p, i) => {
      const auds = auditoriasPorProyecto[i];
      const ordenadas = [...auds].sort(
        (a, b) => fechaMs(b.fechaInicioUtc) - fechaMs(a.fechaInicioUtc));
      const ultima = ordenadas[0] ?? null;
      const completada = ordenadas.find((a) => a.estado === 'Completada') ?? null;

      let cumpl: number | null = null;
      let nc = 0;
      if (completada) {
        const res = await obtenerResultado(completada.id).catch(() => null);
        if (res) {
          const aplic = res.artefactosEvaluados.filter((a) => a.aplica === 'Aplica');
          const conf = aplic.filter((a) => a.resultado === 'Conforme').length;
          const noConf = aplic.filter((a) => a.resultado === 'NoConforme').length;
          gConf += conf; gNoConf += noConf;
          if (conf + noConf > 0) cumpl = Math.round((conf / (conf + noConf)) * 100);
          nc = aplic.reduce(
            (s, a) => s + a.hallazgos.filter((h) => mapTipoFlexible(h.tipo) === 'NC').length, 0);
        }
      }
      return {
        id: p.id,
        nombre: p.nombre,
        procCodigo: procCodigo.get(p.procedimientoId) ?? `Proc #${p.procedimientoId}`,
        cumpl,
        nc,
        estadoUltima: (ultima?.estado ?? 'SinAuditar') as EstadoAuditoria | 'SinAuditar',
        fecha: ultima?.fechaInicioUtc ?? null,
      };
    })
  );

  // Proyectos por estado (de su última auditoría).
  const ppe: ProyectosPorEstado = { completada: 0, enCurso: 0, fallida: 0, sinAuditar: 0 };
  for (const f of filas) {
    if (f.estadoUltima === 'Completada') ppe.completada++;
    else if (f.estadoUltima === 'EnCurso') ppe.enCurso++;
    else if (f.estadoUltima === 'Fallida') ppe.fallida++;
    else ppe.sinAuditar++;
  }

  // Cumplimiento por procedimiento (promedio de los proyectos con cumplimiento).
  const porProc = new Map<string, number[]>();
  for (const f of filas) if (f.cumpl != null) {
    const arr = porProc.get(f.procCodigo) ?? [];
    arr.push(f.cumpl);
    porProc.set(f.procCodigo, arr);
  }
  const cumplPorProcedimiento: CumplProc[] = [...porProc.entries()]
    .map(([procedimiento, arr]) => ({
      procedimiento,
      pct: Math.round(arr.reduce((s, x) => s + x, 0) / arr.length),
    }))
    .sort((a, b) => b.pct - a.pct);

  // Hallazgos por agente.
  const porAgente = new Map<string, number>();
  for (const h of hallazgos) porAgente.set(h.agenteOrigen, (porAgente.get(h.agenteOrigen) ?? 0) + 1);
  const hallazgosPorAgente: ConteoAgente[] = [...porAgente.entries()]
    .map(([agente, cantidad]) => ({ agente, cantidad }))
    .sort((a, b) => b.cantidad - a.cantidad);

  // Evolución de hallazgos por día (cada día con auditorías = un punto).
  const porDia = new Map<string, number>();
  for (const h of hallazgos) {
    const a = auditoriaPorId.get(h.auditoriaId);
    const ms = a ? Date.parse(a.fechaInicioUtc) : NaN;
    if (Number.isNaN(ms)) continue;
    const d = new Date(ms);
    const key = `${d.getFullYear()}-${String(d.getMonth() + 1).padStart(2, '0')}-${String(d.getDate()).padStart(2, '0')}`;
    porDia.set(key, (porDia.get(key) ?? 0) + 1);
  }
  const evolucion: SerieEvolucion[] = [...porDia.entries()]
    .sort((a, b) => (a[0] < b[0] ? -1 : 1))
    .map(([key, valor], i) => {
      const [y, mm, dd] = key.split('-').map(Number);
      const etiqueta = new Date(y, mm - 1, dd).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
      return { etiqueta, valor, orden: i, fecha: key };
    });

  // Últimos hallazgos.
  const ultimosHallazgos: UltimoHallazgo[] = hallazgos
    .map((h) => {
      const a = auditoriaPorId.get(h.auditoriaId);
      return {
        id: h.id,
        descripcion: h.descripcion,
        proyecto: a?.nombreProyecto ?? '—',
        tipo: h.tipo,
        fecha: a?.fechaInicioUtc ?? '',
      };
    })
    .sort((a, b) => fechaMs(b.fecha) - fechaMs(a.fecha) || b.id - a.id)
    .slice(0, MAX_ULTIMOS_HALLAZGOS);

  // Atención: cumplimiento bajo o con NC.
  const atencion = filas
    .filter((f) => (f.cumpl != null && f.cumpl < 70) || f.nc > 0)
    .sort((a, b) => (a.cumpl ?? 999) - (b.cumpl ?? 999));

  const revisadosG = gConf + gNoConf;
  return {
    metricas: calcularMetricas(proyectos.length, auditorias, hallazgos),
    ultimosHallazgos,
    hallazgosPorTipo: {
      nc: contarPorTipo(hallazgos, 'NC'),
      obs: contarPorTipo(hallazgos, 'OBS'),
      om: contarPorTipo(hallazgos, 'OM'),
    },
    proyectosPorEstado: ppe,
    cumplimientoGeneral: {
      revisados: revisadosG, conformes: gConf, noConformes: gNoConf,
      pct: revisadosG > 0 ? Math.round((gConf / revisadosG) * 100) : 0,
    },
    cumplPorProyecto: [...filas].sort((a, b) => (b.cumpl ?? -1) - (a.cumpl ?? -1)),
    cumplPorProcedimiento,
    hallazgosPorAgente,
    evolucion,
    atencion,
  };
}