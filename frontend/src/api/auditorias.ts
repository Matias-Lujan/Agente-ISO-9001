// ============================================================================
//  Endpoints del workflow de auditorias.
//  El usuarioId YA NO se manda en el body — el backend lo extrae del JWT
//  (claim NameIdentifier) del header Authorization.
// ============================================================================

import { api } from './client';

export type EstadoAuditoria = 'EnCurso' | 'Completada' | 'Fallida';

export type NodoWorkflow =
  | 'DocumentAnalysis'
  | 'ComplianceValidation'
  | 'ConsistencyVerification'
  | 'FindingsClassification';

export type EstadoNodo = 'Pendiente' | 'EnCurso' | 'Completado' | 'Fallido';

export interface AuditoriaResumen {
  id: number;
  proyectoId: number;
  etapaId: number;
  usuarioId: number;
  estado: EstadoAuditoria;
  fechaInicioUtc: string;
  fechaFinalizacionUtc: string | null;
}

export interface CrearAuditoriaResponse {
  auditoriaId: number;
  estado: EstadoAuditoria;
}

export interface ProgresoNodo {
  nodo: NodoWorkflow;
  estado: EstadoNodo;
  fechaInicioUtc: string | null;
  fechaFinUtc: string | null;
}

export async function crearAuditoria(
  proyectoId: number,
  etapaId: number
): Promise<CrearAuditoriaResponse> {
  // El backend (POST /api/auditorias) devuelve AuditoriaResponse { id, ..., estado },
  // NO { auditoriaId }. Mapeamos id → auditoriaId para mantener el contrato del front.
  const r = await api.post<{ id: number; estado: EstadoAuditoria }>(
    '/api/auditorias',
    { proyectoId, etapaId }
  );
  return { auditoriaId: r.id, estado: r.estado };
}

export function obtenerAuditoria(id: number): Promise<AuditoriaResumen> {
  return api.get<AuditoriaResumen>(`/api/auditorias/${id}`);
}

// Auditorías de un proyecto (cualquier autenticado). Se usa para armar la
// lista de informes del Operador a partir de sus proyectos asignados.
export function listarAuditoriasDeProyecto(
  proyectoId: number
): Promise<AuditoriaResumen[]> {
  return api.get<AuditoriaResumen[]>(`/api/auditorias/proyecto/${proyectoId}`);
}

export function obtenerProgreso(id: number): Promise<ProgresoNodo[]> {
  return api.get<ProgresoNodo[]>(`/api/auditorias/${id}/progreso`);
}