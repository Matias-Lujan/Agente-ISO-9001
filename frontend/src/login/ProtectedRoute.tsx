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

interface Props {
  children: ReactNode;
  /**
   * Si se especifica, ademas de estar autenticado el usuario debe tener
   * uno de estos roles. Si no, redirige a /nueva-auditoria.
   */
  requiereRol?: Rol | Rol[];
}

export default function ProtectedRoute({ children, requiereRol }: Props) {
  const { estaAutenticado, usuario } = useAuth();
  const location = useLocation();

  // No autenticado → al login
  if (!estaAutenticado || !usuario) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />;
  }

  // Si se requiere rol, validar
  if (requiereRol) {
    const rolesPermitidos = Array.isArray(requiereRol) ? requiereRol : [requiereRol];
    if (!rolesPermitidos.includes(usuario.rol)) {
      return <Navigate to="/nueva-auditoria" replace />;
    }
  }

  return <>{children}</>;
}