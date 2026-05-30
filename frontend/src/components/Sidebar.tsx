
// ============================================================================
//  Sidebar fija a la izquierda.
//
//  Cambios:
//   - El bloque .sb-user muestra el usuario logueado (no "Sin sesion")
//     y un boton para cerrar sesion.
//   - "Hallazgos" esta HABILITADO como NavLink (igual que "Nueva auditoria").
//   - Dashboard / Proyectos / Informes siguen deshabilitados hasta que se
//     desarrollen esas pantallas.
// ============================================================================

import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../login/AuthContext';

export default function Sidebar() {
  const location = useLocation();
  const navigate = useNavigate();
  const { usuario, cerrarSesion } = useAuth();

  const isAuditActive     = location.pathname.startsWith('/nueva-auditoria');
  const isHallazgosActive = location.pathname.startsWith('/hallazgos');

  const handleLogout = () => {
    cerrarSesion();
    navigate('/login', { replace: true });
  };

  // Inicial del avatar (primera letra del nombre)
  const inicial = usuario?.nombre?.trim().charAt(0).toUpperCase() ?? '—';

  return (
    <aside className="sidebar">
      <div className="sb-logo">
        <img src="https://bdtglobal.com/img/logo15horizontal-min.png" alt="bdtglobal" />
      </div>

      {/* Bloque de usuario logueado */}
      <div className="sb-user">
        <div className="sb-avatar">{inicial}</div>
        <div style={{ flex: 1, minWidth: 0 }}>
          <div
            className="sb-name"
            style={{ whiteSpace: 'nowrap', overflow: 'hidden', textOverflow: 'ellipsis' }}
          >
            {usuario?.nombre ?? 'Sin sesión'}
          </div>
          <div className="sb-role">{usuario?.rol ?? ''}</div>
        </div>
        {usuario && (
          <button
            onClick={handleLogout}
            title="Cerrar sesión"
            style={{
              background: 'transparent',
              border: '0.5px solid rgba(255,255,255,0.2)',
              color: 'rgba(255,255,255,0.7)',
              borderRadius: 6,
              padding: '4px 6px',
              fontSize: 11,
              cursor: 'pointer',
            }}
            type="button"
          >
            ⎋
          </button>
        )}
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

      {/* HABILITADO — pantalla nueva */}
      <NavLink
        to="/hallazgos"
        className={`nav-item clickable${isHallazgosActive ? ' active' : ''}`}
      >
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path
            d="M8 2l1.8 3.6L14 6.5l-3 2.9.7 4.1L8 11.4l-3.7 2.1.7-4.1L2 6.5l4.2-.9L8 2z"
            stroke="currentColor"
            strokeWidth="1.2"
          />
        </svg>
        Hallazgos
      </NavLink>
    </aside>
  );
}