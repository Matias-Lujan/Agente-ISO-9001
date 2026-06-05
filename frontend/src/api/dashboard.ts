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
