import type { Rol } from '../login/authApi';

// Pantalla por defecto al ingresar, según el rol.
// El Operador no tiene acceso al Dashboard, así que su inicio es Hallazgos.
// Se usa en el login, en el catch-all de rutas y como fallback de ProtectedRoute
// (evita loops de redirección a una pantalla que el rol no puede ver).
export function rutaInicial(rol?: Rol): string {
  return rol === 'Operador' ? '/hallazgos' : '/dashboard';
}
