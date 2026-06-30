// ============================================================================
//  Cliente del dashboard del auditor.
//
//  La agregación vive en el backend (AnalyticsService → GET /api/dashboard), que
//  es la ÚNICA fuente de verdad de las métricas de hallazgos/cumplimiento. Esta
//  capa solo tipa la respuesta; no calcula nada (antes hacía N+1 requests y
//  duplicaba la lógica, lo que hacía que el dashboard y la pantalla de Hallazgos
//  discreparan).
//
//  Base de cada dato (la fija el backend):
//   - KPIs, donuts, cumplimiento y "atención" → ESTADO ACTUAL (última auditoría
//     Completada de cada proyecto). Cierran con la pantalla de Hallazgos.
//   - Evolución → HISTÓRICO (todas las auditorías Completada, por día).
// ============================================================================

import { api } from './client';

export type EstadoAuditoria = 'EnCurso' | 'Completada' | 'Fallida';
export type TipoHallazgo = 'NC' | 'OBS' | 'OM';

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

export interface UltimoHallazgo {
  id: number;
  descripcion: string;
  proyecto: string;
  tipo: TipoHallazgo;
  fecha: string; // ISO — fecha de la auditoría que lo originó
}

export interface ProyectoCumpl {
  id: number;
  nombre: string;
  procCodigo: string;
  cumpl: number | null;                          // % de la última auditoría completada
  nc: number;                                    // NC de esa ejecución
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

/** Resumen agregado del dashboard. Todo viene calculado del backend. */
export function cargarDashboard(): Promise<DashboardCompleto> {
  return api.get<DashboardCompleto>('/api/dashboard');
}
