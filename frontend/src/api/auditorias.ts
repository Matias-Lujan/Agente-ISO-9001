// src/api/auditorias.ts
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

export function crearAuditoria(
  proyectoId: number,
  etapaId: number
): Promise<CrearAuditoriaResponse> {
  return api.post<CrearAuditoriaResponse>('/api/auditorias', {
    proyectoId,
    etapaId,
  });
}

export function obtenerAuditoria(id: number): Promise<AuditoriaResumen> {
  return api.get<AuditoriaResumen>(`/api/auditorias/${id}`);
}

export function obtenerProgreso(id: number): Promise<ProgresoNodo[]> {
  return api.get<ProgresoNodo[]>(`/api/auditorias/${id}/progreso`);
}