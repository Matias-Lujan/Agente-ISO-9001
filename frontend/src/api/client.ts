// src/api/client.ts
// ============================================================================
//  Wrapper fino sobre fetch. Centraliza:
//   - Base URL relativa (`/api/...`) — el dev server de Vite hace proxy al
//     backend en localhost:5180 (ver vite.config.ts).
//   - Parseo de errores con el body de respuesta cuando el backend devuelve
//     BadRequest / NotFound con texto plano.
//   - Header Authorization automatico cuando hay token en sessionStorage.
//   - Redireccion al /login cuando el backend devuelve 401 (token vencido).
//
//  El backend identifica al usuario por el JWT del header Authorization, asi
//  que ningun endpoint requiere usuarioId en el body.
// ============================================================================

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const token = sessionStorage.getItem('token');

  const res = await fetch(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...(init?.headers ?? {}),
    },
  });

  // 401: token vencido o invalido. Limpiar sesion y mandar al login.
  // No redirigir si ya estamos en /login (evita loops).
  if (res.status === 401 && !window.location.pathname.startsWith('/login')) {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('usuario');
    window.location.href = '/login';
    throw new Error('Sesión expirada. Iniciá sesión de nuevo.');
  }

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