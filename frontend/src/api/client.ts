// ============================================================================
//  Wrapper fino sobre fetch. Centraliza:
//   - Base URL relativa (`/api/...`) — el dev server de Vite hace proxy al
//     backend en localhost:5180 (ver vite.config.ts).
//   - Parseo de errores con el body de respuesta cuando el backend devuelve
//     BadRequest / NotFound con texto plano o JSON.
//   - Envío de la cookie HttpOnly del JWT en cada request (credentials:'include').
//   - Redireccion al /login cuando el backend devuelve 401 (cookie vencida).
//
//  El backend identifica al usuario por el JWT de la cookie 'auth_token', asi
//  que ningun endpoint requiere usuarioId en el body.
// ============================================================================

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const res = await fetch(path, {
    ...init,
    // El JWT vive en una cookie HttpOnly: credentials:'include' hace que el
    // browser la envíe en cada request. No leemos ni guardamos el token en JS.
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      ...(init?.headers ?? {}),
    },
  });

  // 401: cookie vencida o invalida. Mandar al login.
  // No redirigir si ya estamos en /login (evita loops).
  if (res.status === 401 && !window.location.pathname.startsWith('/login')) {
    window.location.href = '/login';
    throw new Error('Sesión expirada. Iniciá sesión de nuevo.');
  }

  if (!res.ok) {
    const detalle = await res.text().catch(() => '');
    // Intentar parsear como JSON (el backend devuelve { mensaje: "..." })
    let mensaje = detalle;
    try {
      const json = JSON.parse(detalle);
      if (json && typeof json === 'object' && 'mensaje' in json) {
        mensaje = String(json.mensaje);
      }
    } catch {
      // No era JSON, dejamos el texto plano
    }
    throw new Error(
      `${res.status} ${res.statusText}${mensaje ? ` — ${mensaje}` : ''}`
    );
  }

  if (res.status === 204) return undefined as T;

  return (await res.json()) as T;
}

export const api = {
  get: <T>(path: string) => request<T>(path, { method: 'GET' }),
  post: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'POST', body: JSON.stringify(body) }),
  put: <T>(path: string, body: unknown) =>
    request<T>(path, { method: 'PUT', body: JSON.stringify(body) }),
};