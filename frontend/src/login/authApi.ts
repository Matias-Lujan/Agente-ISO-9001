// src/login/authApi.ts
// ============================================================================
//  Endpoints de autenticacion. Usa el `api` ya definido en ../api/client.ts
//  para mantener consistencia con auditorias.ts, proyectos.ts, etc.
//
//  El backend corre en localhost:5180. El proxy de Vite (vite.config.ts)
//  redirige /api/* hacia alli, asi que solo usamos rutas relativas.
// ============================================================================

import { api } from '../api/client';

export type Rol = 'Administrador' | 'Auditor' | 'Operador';

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  nombre: string;
  email: string;
  rol: Rol;
  expiracion: string; // ISO date string
}

export interface PerfilUsuario {
  id: number;
  nombre: string;
  email: string;
  rol: Rol;
  activo: boolean;
  fechaCreacion: string;
}

export function login(req: LoginRequest): Promise<LoginResponse> {
  return api.post<LoginResponse>('/api/auth/login', req);
}

export function obtenerPerfil(): Promise<PerfilUsuario> {
  return api.get<PerfilUsuario>('/api/auth/me');
}