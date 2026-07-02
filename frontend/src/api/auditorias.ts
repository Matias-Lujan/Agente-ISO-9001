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
  // Resumen del fallo cuando estado === 'Fallida' (null en otros casos).
  categoriaError: string | null;
  mensajeError: string | null;
}

// Una entrada del log durable de errores de una auditoría.
export interface RegistroError {
  id: number;
  nodo: NodoWorkflow | null;
  categoria: string;
  mensaje: string;
  fechaUtc: string;
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

// Auditorías de un proyecto (cualquier autenticado). Se usa tanto para el
// cálculo de cumplimiento como para el detalle del proyecto.
export interface AuditoriaDeProyecto {
  id: number;
  proyectoId: number;
  nombreProyecto: string;
  usuarioId: number;
  nombreAuditor: string;
  etapaId: number;
  fechaInicioUtc: string;
  fechaFinalizacionUtc: string | null;
  estado: EstadoAuditoria;
  // Resumen del fallo cuando estado === 'Fallida' (null en otros casos).
  categoriaError: string | null;
  mensajeError: string | null;
}

export function listarAuditoriasDeProyecto(
  proyectoId: number
): Promise<AuditoriaDeProyecto[]> {
  return api.get<AuditoriaDeProyecto[]>(`/api/auditorias/proyecto/${proyectoId}`);
}

// ── Resultado de una auditoría (artefactos evaluados) ──
// Sirve para calcular el % de cumplimiento de un proyecto.
export type AplicaArtefacto = 'Aplica' | 'NoAplica' | 'SinDeclararEnTailoring';
export type ResultadoArtefacto =
  | 'Conforme' | 'NoConforme' | 'NoAplica' | 'PendienteEtapaFutura';

export interface HallazgoResultado {
  id: number;
  tipo: string;          // 'NoConformidad' | 'Observacion' | 'OportunidadMejora' ...
  descripcion: string;
  justificacion: string;
  agenteOrigen: string;
}

export interface DocumentoAnalizadoResultado {
  id: number;
  nombreArchivo: string;
  fuente: string;
  urlReferencia: string | null;
  hashContenido: string;
}

export interface ArtefactoEvaluado {
  id: number;
  artefactoEsperadoId: number;
  artefactoEsperadoCodigo: string | null;
  artefactoEsperadoNombre: string | null;
  aplica: AplicaArtefacto;
  resultado: ResultadoArtefacto;
  observaciones: string | null;
  hallazgos: HallazgoResultado[];
  documentosAnalizados: DocumentoAnalizadoResultado[];
}

export interface AuditoriaResultado {
  id: number;
  proyectoId: number;
  etapaId: number;
  usuarioId: number;
  estado: EstadoAuditoria;
  contadores: { artefactosEvaluados: number; hallazgos: number; documentosAnalizados: number };
  artefactosEvaluados: ArtefactoEvaluado[];
}

export function obtenerResultado(id: number): Promise<AuditoriaResultado> {
  return api.get<AuditoriaResultado>(`/api/auditorias/${id}/resultado`);
}

export function obtenerProgreso(id: number): Promise<ProgresoNodo[]> {
  return api.get<ProgresoNodo[]>(`/api/auditorias/${id}/progreso`);
}

// Log de errores de una auditoría (motivo del fallo y en qué nodo). Más
// reciente primero. Lista vacía si la auditoría no tuvo errores.
export function obtenerErrores(id: number): Promise<RegistroError[]> {
  return api.get<RegistroError[]>(`/api/auditorias/${id}/errores`);
}