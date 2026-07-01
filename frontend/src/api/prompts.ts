// ============================================================================
//  System prompts de los agentes (solo Administrador).
//  El backend versiona cada prompt: cada guardado crea una versión nueva y
//  activa; el historial permite revertir. Editar mal un prompt puede romper el
//  pipeline de auditoría, por eso estos endpoints son admin-only.
// ============================================================================

import { api } from './client';

export interface PromptVersion {
  version: number;
  esActiva: boolean;
  modificadoPorUsuarioId: number | null;
  modificadoPorNombre: string | null;
  fechaCreacion: string;
  comentario: string | null;
}

export interface PromptAgente {
  agenteKey: string;
  contenido: string;
  esDefault: boolean;      // el contenido activo coincide con el default en código
  versionActiva: number;
  historial: PromptVersion[];
}

export function obtenerPrompt(agenteKey: string): Promise<PromptAgente> {
  return api.get<PromptAgente>(`/api/config/prompts/${agenteKey}`);
}

export function actualizarPrompt(
  agenteKey: string,
  contenido: string,
  comentario: string | null,
): Promise<PromptAgente> {
  return api.put<PromptAgente>(`/api/config/prompts/${agenteKey}`, { contenido, comentario });
}

export function restablecerPrompt(agenteKey: string): Promise<PromptAgente> {
  return api.post<PromptAgente>(`/api/config/prompts/${agenteKey}/reset`, {});
}

export function revertirPrompt(agenteKey: string, version: number): Promise<PromptAgente> {
  return api.post<PromptAgente>(`/api/config/prompts/${agenteKey}/revert/${version}`, {});
}
