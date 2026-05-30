
// ============================================================================
//  Router principal.
//
//  Rutas:
//   - /login              Publica (sin sidebar)
//   - /nueva-auditoria    Protegida (con sidebar)
//   - /hallazgos          Protegida (con sidebar)  ← NUEVA
//   - *                   Redirige a /nueva-auditoria
// ============================================================================

import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import type { ReactNode } from 'react';
import Sidebar from './components/Sidebar';
import NuevaAuditoria from './screens/NuevaAuditoria';
import Hallazgos from './screens/Hallazgos';
import { useInjectStyle } from './utils/useInjectStyle';
import { sharedCss } from './styles/shared';
import { AuthProvider } from './login/AuthContext';
import ProtectedRoute from './login/ProtectedRoute';
import Login from './login/Login';

// Shell con sidebar — para las rutas protegidas
function ShellLayout({ children }: { children: ReactNode }) {
  return (
    <div className="shell">
      <Sidebar />
      <main className="main">{children}</main>
    </div>
  );
}

export default function App() {
  useInjectStyle(sharedCss, 'app-shared-style');

  return (
    <AuthProvider>
      <BrowserRouter>
        <Routes>
          {/* Ruta publica */}
          <Route path="/login" element={<Login />} />

          {/* Rutas protegidas */}
          <Route
            path="/nueva-auditoria"
            element={
              <ProtectedRoute>
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

          {/* Catch-all */}
          <Route path="*" element={<Navigate to="/nueva-auditoria" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}