// ============================================================================
//  Endpoints de proyectos.
//
//  Visibilidad por rol (la resuelve el backend según el JWT de la cookie):
//   - Administrador / Auditor → GET /api/proyectos devuelve TODOS los proyectos
//   - Operador                → solo los proyectos que tiene asignados
//
//  Crear / asignar responsables son solo-Administrador (el backend lo valida con
//  [Authorize(Roles = Administrador)]; el front además oculta el botón/modal).
// ============================================================================
import { api } from './client';
import type { UsuarioResponse } from '../login/authApi';

// Espejo de ProyectoResponse del backend.
export interface Proyecto {
  id: number;
  nombre: string;
  descripcion: string | null;
  fechaInicio: string | null;
  fechaFin: string | null;
  tipoProyecto: string;            // "A" | "B"
  horasEstimadas: number | null;
  procedimientoId: number;
  trelloBoardId: string | null;
  clockifyProjectId: string | null;
  driveFolderId: string | null;
  activo: boolean;
  responsables: UsuarioResponse[];  // usuarios asignados al proyecto
}

// Espejo de CrearProyectoRequest del backend.
export interface CrearProyectoRequest {
  nombre: string;
  descripcion?: string | null;
  fechaInicio?: string | null;
  fechaFin?: string | null;
  tipoProyecto: string;            // "A" | "B"
  horasEstimadas?: number | null;
  procedimientoId: number;
  trelloBoardId?: string | null;
  clockifyProjectId?: string | null;
  driveFolderId?: string | null;
}

export function listarProyectos(): Promise<Proyecto[]> {
  return api.get<Proyecto[]>('/api/proyectos');
}

export function obtenerProyecto(id: number): Promise<Proyecto> {
  return api.get<Proyecto>(`/api/proyectos/${id}`);
}

// Solo Administrador.
export function crearProyecto(req: CrearProyectoRequest): Promise<Proyecto> {
  return api.post<Proyecto>('/api/proyectos', req);
}

// Solo Administrador. Asigna un usuario (normalmente Operador) al proyecto.
export function asignarResponsable(
  proyectoId: number,
  usuarioId: number,
): Promise<{ mensaje: string }> {
  return api.post<{ mensaje: string }>(
    `/api/proyectos/${proyectoId}/responsables`,
    { usuarioId },
  );
}