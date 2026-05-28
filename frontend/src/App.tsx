import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom';
import Sidebar from './components/Sidebar';
import NuevaAuditoria from './screens/NuevaAuditoria';
import { useInjectStyle } from './utils/useInjectStyle';
import { sharedCss } from './styles/shared';

// Shell de la app: sidebar fija + main panel ruteado.
// Solo hay una ruta real (/nueva-auditoria); el resto redirige.
// Cuando se agreguen Dashboard, Proyectos, Informes y Hallazgos, se enchufan
// como nuevos <Route> sin tocar el sidebar más que para habilitar el NavLink.
export default function App() {
  useInjectStyle(sharedCss, 'app-shared-style');

  return (
    <BrowserRouter>
      <div className="shell">
        <Sidebar />
        <main className="main">
          <Routes>
            <Route path="/nueva-auditoria" element={<NuevaAuditoria />} />
            <Route path="*" element={<Navigate to="/nueva-auditoria" replace />} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  );
}
