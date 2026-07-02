// ============================================================================
//  Módulo de hallazgos — cliente de la revisión consolidada.
//
//  La agregación vive en el backend (AnalyticsService → GET /api/hallazgos/revision),
//  que es la misma fuente de verdad que el dashboard: así los conteos no pueden
//  discrepar entre pantallas. Antes esta capa hacía N+1 requests y recalculaba
//  todo en el cliente.
//
//  Alcance (lo elige la pantalla):
//   - actual    → última auditoría Completada de cada proyecto (estado actual).
//   - historico → todas las auditorías Completada (acumulado en el tiempo).
//
//  El backend ya scopea por rol (el Operador solo ve sus proyectos asignados).
// ============================================================================

import { api } from './client';

export type TipoHallazgo = 'NoConformidad' | 'Observacion' | 'OportunidadMejora';
export type EstadoHallazgo = 'Abierto' | 'EnRevision' | 'Resuelto';
export type AlcanceRevision = 'actual' | 'historico';

export interface Hallazgo {
  id: number;
  titulo: string;
  descripcion: string;
  evidencia: string;
  proyecto: string;
  tipo: TipoHallazgo;
  estado: EstadoHallazgo;
  fechaDeteccion: string;     // ISO date
  agenteOrigen?: string;
  justificacion?: string;
}

export interface ItemConforme {
  id: number;
  codigo: string | null;
  nombre: string;
  proyecto: string;
}

export interface Revision {
  hallazgos: Hallazgo[];
  conformes: ItemConforme[];
  revisados: number;          // artefactos aplicables con veredicto
  totalConformes: number;
  totalNoConformes: number;
}

// ── Helpers de presentación (label + color) ───────────────────────────────────

export const TIPO_LABEL: Record<TipoHallazgo, string> = {
  NoConformidad:     'NC',
  Observacion:       'OBS',
  OportunidadMejora: 'OM',
};

export const TIPO_LABEL_LARGO: Record<TipoHallazgo, string> = {
  NoConformidad:     'No conformidad',
  Observacion:       'Observación',
  OportunidadMejora: 'Oportunidad de mejora',
};

export const ESTADO_LABEL: Record<EstadoHallazgo, string> = {
  Abierto:    'Abierto',
  EnRevision: 'En revisión',
  Resuelto:   'Resuelto',
};

// ── Shape tal cual lo devuelve el backend (DTOs/DashboardDTOs.cs) ─────────────

type TipoCorto = 'NC' | 'OBS' | 'OM';

interface RevisionHallazgoApi {
  id: number;
  titulo: string;
  descripcion: string;
  evidencia: string;
  proyecto: string;
  tipo: TipoCorto;
  fecha: string;
  agenteOrigen: string;
}

interface RevisionConformeApi {
  id: number;
  codigo: string | null;
  nombre: string;
  proyecto: string;
}

interface RevisionApi {
  hallazgos: RevisionHallazgoApi[];
  conformes: RevisionConformeApi[];
  revisados: number;
  totalConformes: number;
  totalNoConformes: number;
}

const TIPO_DESDE_CORTO: Record<TipoCorto, TipoHallazgo> = {
  NC:  'NoConformidad',
  OBS: 'Observacion',
  OM:  'OportunidadMejora',
};

// Hallazgos crudos de UNA auditoría (GET /api/hallazgos/auditoria/{id}).
interface HallazgoAuditoriaApi {
  id: number;
  auditoriaId: number;
  artefactoEvaluadoId: number;
  nombreArtefacto: string;
  tipo: TipoCorto;
  descripcion: string;
  justificacion: string;
  agenteOrigen: string;
}

/**
 * Hallazgos de una auditoría puntual, mapeados al shape `Hallazgo`. Se usa para
 * armar el PDF del informe (el campo `proyecto` lo completa quien llama, que es
 * el que conoce el nombre del proyecto del informe).
 */
export async function listarHallazgosDeAuditoria(auditoriaId: number): Promise<Hallazgo[]> {
  const r = await api.get<HallazgoAuditoriaApi[]>(`/api/hallazgos/auditoria/${auditoriaId}`);
  return r.map((h) => ({
    id: h.id,
    titulo: h.nombreArtefacto,
    descripcion: h.descripcion,
    evidencia: h.justificacion ?? '',
    proyecto: '',
    tipo: TIPO_DESDE_CORTO[h.tipo] ?? 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '',
    agenteOrigen: h.agenteOrigen,
    justificacion: h.justificacion,
  }));
}

/**
 * Trae la revisión consolidada de hallazgos para el alcance pedido.
 * El backend resuelve el scope por rol y aplica la regla canónica.
 */
export async function cargarRevision(scope: AlcanceRevision = 'actual'): Promise<Revision> {
  const r = await api.get<RevisionApi>(`/api/hallazgos/revision?scope=${scope}`);
  return {
    hallazgos: r.hallazgos.map((h) => ({
      id: h.id,
      titulo: h.titulo,
      descripcion: h.descripcion,
      evidencia: h.evidencia,
      proyecto: h.proyecto,
      tipo: TIPO_DESDE_CORTO[h.tipo] ?? 'Observacion',
      estado: 'Abierto',
      fechaDeteccion: h.fecha,
      agenteOrigen: h.agenteOrigen,
      justificacion: h.evidencia,
    })),
    conformes: r.conformes.map((c) => ({
      id: c.id, codigo: c.codigo, nombre: c.nombre, proyecto: c.proyecto,
    })),
    revisados: r.revisados,
    totalConformes: r.totalConformes,
    totalNoConformes: r.totalNoConformes,
  };
}
