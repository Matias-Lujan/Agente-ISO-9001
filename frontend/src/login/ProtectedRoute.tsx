// src/login/ProtectedRoute.tsx
// ============================================================================
//  HOC que envuelve rutas protegidas.
//
//  Niveles de proteccion:
//   1. Por defecto: solo requiere estar autenticado
//   2. requiereRol: ademas requiere un rol especifico (ej: "Administrador")
//
//  Si no esta autenticado:
//   → redirige a /login guardando la ruta original en state.from
//
//  Si esta autenticado pero NO tiene el rol requerido:
//   → redirige a /nueva-auditoria (ruta segura por defecto)
// ============================================================================
import { Navigate, useLocation } from 'react-router-dom';
import { useAuth } from './AuthContext';
import type { ReactNode } from 'react';
import type { Rol } from './authApi';
import { rutaInicial } from '../utils/navegacion';

interface Props {
  children: ReactNode;
  /**
   * Si se especifica, ademas de estar autenticado el usuario debe tener
   * uno de estos roles. Si no, redirige a /nueva-auditoria.
   */
  requiereRol?: Rol | Rol[];
}

export default function ProtectedRoute({ children, requiereRol }: Props) {
  const { estaAutenticado, usuario, verificandoSesion } = useAuth();
  const location = useLocation();

  // Mientras se verifica la cookie de sesion (GET /api/auth/me al montar), no
  // decidimos nada para no rebotar al login antes de saber si hay sesion.
  if (verificandoSesion) {
    return <div className="loading-text">Verificando sesión…</div>;
  }

  // No autenticado → al login
  if (!estaAutenticado || !usuario) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  // Si se requiere rol, validar
  if (requiereRol) {
    const rolesPermitidos = Array.isArray(requiereRol) ? requiereRol : [requiereRol];
    if (!rolesPermitidos.includes(usuario.rol)) {
      // A la pantalla inicial que el rol sí puede ver (evita loops de redirección).
      return <Navigate to={rutaInicial(usuario.rol)} replace />;
    }
  }

  return <>{children}</>;
}