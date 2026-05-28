// Wrapper fino sobre fetch. Centraliza:
//  - Base URL relativa (`/api/...`) — el dev server de Vite hace proxy al
//    backend en localhost:5180 (ver vite.config.ts).
//  - Parseo de errores con el body de respuesta cuando el backend devuelve
//    BadRequest / NotFound con texto plano.
//
// TEMPORAL hasta tener login: DEFAULT_USUARIO_ID se manda como usuarioId en
// POST /api/auditorias. El backend ya tiene un comentario explícito de que
// usuarioId del body es transitorio hasta que se agregue JWT. Cuando exista
// autenticación, esta constante desaparece y el id sale del contexto del
// usuario logueado.
export const DEFAULT_USUARIO_ID = 1;

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  });

  if (!res.ok) {
    const detalle = await res.text().catch(() => '');
    throw new Error(
      `${res.status} ${res.statusText}${detalle ? ` — ${detalle}` : ''}`
    );
  }

  if (res.status === 204) return undefined as T;

  return (await res.json()) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
};
