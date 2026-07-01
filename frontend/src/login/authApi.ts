// ============================================================================
//  Endpoints de autenticacion y gestion de usuarios.
//  Usa el `api` definido en ../api/client.ts para consistencia.
//
//  Backend corre en localhost:5180. El proxy de Vite redirige /api/* alli.
// ============================================================================

import { api } from '../api/client';

// ── Tipos ────────────────────────────────────────────────────────────────────

export type Rol = 'Administrador' | 'Auditor' | 'Operador';

export type Tema = 'claro' | 'oscuro';

export interface LoginRequest {
  email: string;
  password: string;
}

// El token NO viaja en el body: vive en una cookie HttpOnly que setea el backend.
export interface LoginResponse {
  nombre: string;
  email: string;
  rol: Rol;
  expiracion: string;
}

export interface PerfilUsuario {
  id: number;
  nombre: string;
  email: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
  tema: Tema;
}

// Para el ABM
export interface UsuarioResponse {
  id: number;
  nombre: string;
  email: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
}

export interface CrearUsuarioRequest {
  nombre: string;
  email: string;
  password: string;
  rol: Rol;
}

export interface ModificarUsuarioRequest {
  nombre?: string | null;
  email?: string | null;
  rol?: Rol | null;
  activo?: boolean | null;
}

export interface ResetearPasswordRequest {
  passwordNueva: string;
}

// El usuario logueado cambia SU propia contraseña: exige la actual.
export interface CambiarPasswordRequest {
  passwordActual: string;
  passwordNueva: string;
}

// ── Endpoints de auth ────────────────────────────────────────────────────────

export function login(req: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>('/api/auth/login', req);
}

export function obtenerPerfil(): Promise<PerfilUsuario> {
  return api.get<PerfilUsuario>('/api/auth/me');
}

// Cierra la sesion: el backend borra la cookie HttpOnly del JWT.
export function logout(): Promise<void> {
  return api.post<void>('/api/auth/logout', {});
}

// Guarda la preferencia de tema del usuario logueado (se persiste por usuario).
export function guardarTema(tema: Tema): Promise<void> {
  return api.put<void>('/api/auth/me/tema', { tema });
}

// El usuario logueado cambia su propia contraseña (verifica la actual en el backend).
export function cambiarPassword(req: CambiarPasswordRequest): Promise<void> {
  return api.put<void>('/api/auth/me/password', req);
}

// ── Endpoints del ABM (solo Administrador) ───────────────────────────────────

export function listarUsuarios(): Promise<UsuarioResponse[]> {
  return api.get<UsuarioResponse[]>('/api/auth/usuarios');
}

export function obtenerUsuarioPorId(id: number): Promise<UsuarioResponse> {
  return api.get<UsuarioResponse>(`/api/auth/usuarios/${id}`);
}

export function crearUsuario(req: CrearUsuarioRequest): Promise<UsuarioResponse> {
  return api.post<UsuarioResponse>('/api/auth/usuarios', req);
}

export function modificarUsuario(
  id: number,
  req: ModificarUsuarioRequest,
): Promise<UsuarioResponse> {
  return api.put<UsuarioResponse>(`/api/auth/usuarios/${id}`, req);
}

export function resetearPassword(
  id: number,
  req: ResetearPasswordRequest,
): Promise<void> {
  return api.put<void>(`/api/auth/usuarios/${id}/password`, req);
}