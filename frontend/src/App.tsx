// src/App.tsx
// ============================================================================
//  Router principal.
//
//  Rutas:
//   - /login              Publica (sin sidebar)
//   - /dashboard          Protegida — Administrador / Auditor (NO Operador)
//   - /nueva-auditoria    Protegida — Administrador / Auditor (NO Operador)
//   - /hallazgos          Protegida — cualquier rol autenticado
//   - /configuracion      Protegida — cualquier rol autenticado
//   - /usuarios           Protegida — SOLO Administrador (ABM)
//   - *                   Redirige a la pantalla inicial según el rol
// ============================================================================
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import Sidebar from './components/Sidebar';
import Dashboard from './screens/Dashboard';
import Proyectos from './screens/Proyectos';
import ProyectoDetalle from './screens/ProyectoDetalle';
import Informes from './screens/Informes';
import NuevaAuditoria from './screens/NuevaAuditoria';
import Hallazgos from './screens/Hallazgos';
import Configuracion from './screens/Configuracion';
import Usuarios from './screens/Usuarios';
import { useInjectStyle } from './utils/useInjectStyle';
import { sharedCss } from './styles/shared';
import { AuthProvider, useAuth } from './login/AuthContext';
import ProtectedRoute from './login/ProtectedRoute';
import Login from './login/Login';
import { rutaInicial } from './utils/navegacion';

// Shell con sidebar — para las rutas protegidas
function ShellLayout({ children }: { children: ReactNode }) {
  return (
    <div className="shell">
      <Sidebar />
      <main className="main">{children}</main>
    </div>
  );
}

// Redirección del catch-all a la pantalla inicial que corresponde al rol
// (Operador → /hallazgos, resto → /dashboard).
function InicioRedirect() {
  const { usuario } = useAuth();
  return <Navigate to={rutaInicial(usuario?.rol)} replace />;
}

export default function App() {
  useInjectStyle(sharedCss, 'app-shared-style');

  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Ruta publica */}
          <Route path="/login" element={<Login />} />

          {/* Rutas protegidas — cualquier rol autenticado */}
          <Route
            path="/proyectos/:id"
            element={
              <ProtectedRoute>
                <ShellLayout>
                  <ProyectoDetalle />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/proyectos"
            element={
              <ProtectedRoute>
                <ShellLayout>
                  <Proyectos />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/informes"
            element={
              <ProtectedRoute>
                <ShellLayout>
                  <Informes />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute requiereRol={['Administrador', 'Auditor']}>
                <ShellLayout>
                  <Dashboard />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/nueva-auditoria"
            element={
              <ProtectedRoute requiereRol={['Administrador', 'Auditor']}>
                <ShellLayout>
                  <NuevaAuditoria />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/hallazgos"
            element={
              <ProtectedRoute>
                <ShellLayout>
                  <Hallazgos />
                </ShellLayout>
              </ProtectedRoute>
            }
          />
          <Route
            path="/configuracion"
            element={
              <ProtectedRoute>
                <ShellLayout>
                  <Configuracion />
                </ShellLayout>
              </ProtectedRoute>
            }
          />

          {/* Ruta protegida — solo Administrador */}
          <Route
            path="/usuarios"
            element={
              <ProtectedRoute requiereRol="Administrador">
                <ShellLayout>
                  <Usuarios />
                </ShellLayout>
              </ProtectedRoute>
            }
          />

          {/* Catch-all — a la pantalla inicial según el rol */}
          <Route path="*" element={<InicioRedirect />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}