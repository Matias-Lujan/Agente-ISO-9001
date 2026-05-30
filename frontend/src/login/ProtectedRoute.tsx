// src/login/ProtectedRoute.tsx
// ============================================================================
//  HOC para rutas que requieren sesion. Si no hay login, redirige a /login
//  guardando la ruta original en state para volver despues del login.
// ============================================================================

import { Navigate, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAuth } from './AuthContext';

export default function ProtectedRoute({ children }: { children: ReactNode }) {
  const { estaAutenticado } = useAuth();
  const location = useLocation();

  if (!estaAutenticado) {
    return <Navigate to="/login" state={{ from: location }} replace />;
  }

  return <>{children}</>;
}