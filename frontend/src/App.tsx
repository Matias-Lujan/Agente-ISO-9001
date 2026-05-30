// src/App.tsx
// ============================================================================
//  Shell de la app con autenticacion.
//
//  Diferencias con el App.tsx original:
//   - AuthProvider envolviendo todo.
//   - Ruta /login PUBLICA (no usa el shell con sidebar — ocupa toda la pantalla).
//   - Ruta /nueva-auditoria PROTEGIDA por ProtectedRoute.
//
//  El shell (sidebar + main) solo se renderiza dentro de las rutas protegidas.
//  Asi el login no tiene sidebar al costado.
// ============================================================================

import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import NuevaAuditoria from './screens/NuevaAuditoria';
import { useInjectStyle } from './utils/useInjectStyle';
import { sharedCss } from './styles/shared';
import { AuthProvider } from './login/AuthContext';
import ProtectedRoute from './login/ProtectedRoute';
import Login from './login/Login';

// Shell con sidebar — para las rutas protegidas
function ShellLayout({ children }: { children: React.ReactNode }) {
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
          {/* Ruta publica: login (sin sidebar) */}
          <Route path="/login" element={<Login />} />

          {/* Rutas protegidas: requieren sesion, muestran sidebar */}
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

          {/* Catch-all: cualquier otra ruta va a /nueva-auditoria.
              Si no esta logueado, ProtectedRoute lo manda a /login. */}
          <Route path="*" element={<Navigate to="/nueva-auditoria" replace />} />
        </Routes>
      </BrowserRouter>
    </AuthProvider>
  );
}