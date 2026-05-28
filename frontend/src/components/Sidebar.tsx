import { NavLink, useLocation } from 'react-router-dom';

// Sidebar fija a la izquierda. Sigue la maqueta del prototipo
// (untitled/src/components/NewAudit.tsx). Diferencias con el prototipo:
//   - El bloque de usuario muestra un placeholder ("Sin sesión") hasta que
//     haya login. El nombre hardcodeado "Matías Luján" se sacó.
//   - De las 5 opciones de navegación SOLO "Nueva auditoría" es clickeable.
//     Las otras 4 (Dashboard, Proyectos, Informes, Hallazgos) se muestran con
//     opacidad reducida para indicar que están deshabilitadas — se cablearán
//     cuando se desarrollen esas pantallas.
export default function Sidebar() {
  const location = useLocation();
  const isAuditActive = location.pathname.startsWith('/nueva-auditoria');

  return (
    <aside className="sidebar">
      <div className="sb-logo">
        <img src="/logo15horizontal-min.png" alt="bdtglobal" />
      </div>

      <div className="sb-user">
        <div className="sb-avatar">—</div>
        <div className="sb-user-placeholder">Sin sesión</div>
      </div>

      <div className="nav-item disabled" title="Próximamente">
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <rect x="1" y="1" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
          <rect x="9" y="1" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
          <rect x="1" y="9" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
          <rect x="9" y="9" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
        </svg>
        Dashboard
      </div>

      <div className="nav-item disabled" title="Próximamente">
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M2 4h12M2 8h12M2 12h7" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
        </svg>
        Proyectos
      </div>

      <NavLink
        to="/nueva-auditoria"
        className={`nav-item clickable${isAuditActive ? ' active' : ''}`}
      >
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <circle cx="8" cy="8" r="6" stroke="currentColor" strokeWidth="1.2" />
          <path d="M8 5v3l2 2" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
        </svg>
        Nueva auditoría
      </NavLink>

      <div className="nav-item disabled" title="Próximamente">
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M3 2h7l3 3v9H3V2z" stroke="currentColor" strokeWidth="1.2" />
          <path d="M10 2v3h3M5 7h6M5 10h4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
        </svg>
        Informes
      </div>

      <div className="nav-item disabled" title="Próximamente">
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M8 2l1.8 3.6L14 6.5l-3 2.9.7 4.1L8 11.4l-3.7 2.1.7-4.1L2 6.5l4.2-.9L8 2z" stroke="currentColor" strokeWidth="1.2" />
        </svg>
        Hallazgos
      </div>
    </aside>
  );
}
