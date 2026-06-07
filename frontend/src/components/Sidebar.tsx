// src/components/Sidebar.tsx
// ============================================================================
//  Sidebar fija a la izquierda.
//
//  Cambios:
//   - El bloque .sb-user (usuario logueado) es clickeable y abre Configuración,
//     con el mismo hover/activo que el resto de los items del sidebar.
//   - "Cerrar sesión" va al fondo (donde antes estaba Configuración).
//   - "Hallazgos" HABILITADO como NavLink.
//   - "Usuarios" SOLO visible si el rol es Administrador.
//   - Proyectos / Informes siguen deshabilitados.
// ============================================================================
import { Link, NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../login/AuthContext';

export default function Sidebar() {
  const location = useLocation();
  const navigate = useNavigate();
  const { usuario, cerrarSesion } = useAuth();

  const isDashboardActive = location.pathname.startsWith('/dashboard');
  const isProyectosActive = location.pathname.startsWith('/proyectos');
  const isAuditActive     = location.pathname.startsWith('/nueva-auditoria');
  const isHallazgosActive = location.pathname.startsWith('/hallazgos');
  const isUsuariosActive  = location.pathname.startsWith('/usuarios');
  const isConfigActive    = location.pathname.startsWith('/configuracion');

  const esAdmin = usuario?.rol === 'Administrador';
  // Solo Administrador y Auditor pueden ejecutar auditorías y ver el dashboard.
  // El Operador solo accede a Hallazgos (y a su Configuración).
  const puedeAuditar = usuario?.rol === 'Administrador' || usuario?.rol === 'Auditor';
  const puedeVerDashboard = usuario?.rol !== 'Operador';

  const handleLogout = () => {
    cerrarSesion();
    navigate('/login', { replace: true });
  };

  // Inicial del avatar (primera letra del nombre)
  const inicial = usuario?.nombre?.trim().charAt(0).toUpperCase() ?? '—';

  return (
    <aside className="sidebar">
      <Link to="/dashboard" className="sb-logo" title="Ir al dashboard">
        <img src="/logo15horizontal-min.png" alt="bdtglobal" />
      </Link>

      {/* Bloque de usuario logueado → abre Configuración (como un item más) */}
      <NavLink
        to="/configuracion"
        className={`sb-user${isConfigActive ? ' active' : ''}`}
        title="Configuración"
      >
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
      </NavLink>

      {puedeVerDashboard && (
        <NavLink
          to="/dashboard"
          className={`nav-item clickable${isDashboardActive ? ' active' : ''}`}
        >
          <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
            <rect x="1" y="1" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
            <rect x="9" y="1" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
            <rect x="1" y="9" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
            <rect x="9" y="9" width="6" height="6" rx="1.5" stroke="currentColor" strokeWidth="1.2" />
          </svg>
          Dashboard
        </NavLink>
      )}

      <NavLink
        to="/proyectos"
        className={`nav-item clickable${isProyectosActive ? ' active' : ''}`}
      >
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M2 4h12M2 8h12M2 12h7" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
        </svg>
        Proyectos
      </NavLink>

      {puedeAuditar && (
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
      )}

      <div className="nav-item disabled" title="Próximamente">
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M3 2h7l3 3v9H3V2z" stroke="currentColor" strokeWidth="1.2" />
          <path d="M10 2v3h3M5 7h6M5 10h4" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
        </svg>
        Informes
      </div>

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

      {/* USUARIOS — solo visible si es Administrador */}
      {esAdmin && (
        <NavLink
          to="/usuarios"
          className={`nav-item clickable${isUsuariosActive ? ' active' : ''}`}
        >
          <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
            <circle cx="8" cy="5" r="2.5" stroke="currentColor" strokeWidth="1.2" />
            <path d="M3 14c0-2.8 2.2-5 5-5s5 2.2 5 5" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
          </svg>
          Usuarios
        </NavLink>
      )}

      {/* CERRAR SESIÓN — al fondo, separado del resto */}
      <button
        type="button"
        onClick={handleLogout}
        className="nav-item clickable"
        style={{
          marginTop: 'auto',
          background: 'transparent',
          border: 'none',
          borderTop: '0.5px solid var(--sb-border)',
          paddingTop: '14px',
          width: '100%',
          textAlign: 'left',
          fontFamily: 'inherit',
          cursor: 'pointer',
        }}
      >
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <path d="M6 2H3.5A1.5 1.5 0 0 0 2 3.5v9A1.5 1.5 0 0 0 3.5 14H6" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
          <path d="M10 11l3-3-3-3M13 8H6" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" />
        </svg>
        Cerrar sesión
      </button>
    </aside>
  );
}