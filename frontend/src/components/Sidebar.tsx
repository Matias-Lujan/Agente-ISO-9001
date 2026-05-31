// src/components/Sidebar.tsx
// ============================================================================
//  Sidebar fija a la izquierda.
//
//  Cambios:
//   - Bloque .sb-user con datos del usuario logueado + boton logout.
//   - "Hallazgos" HABILITADO como NavLink.
//   - "Usuarios" SOLO visible si el rol es Administrador.
//   - "Configuracion" HABILITADO al fondo (margin-top:auto).
//   - Dashboard / Proyectos / Informes siguen deshabilitados.
// ============================================================================
import React from 'react';
import { NavLink, useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../login/AuthContext';

export default function Sidebar() {
  const location = useLocation();
  const navigate = useNavigate();
  const { usuario, cerrarSesion } = useAuth();

  const isAuditActive     = location.pathname.startsWith('/nueva-auditoria');
  const isHallazgosActive = location.pathname.startsWith('/hallazgos');
  const isUsuariosActive  = location.pathname.startsWith('/usuarios');
  const isConfigActive    = location.pathname.startsWith('/configuracion');

  const esAdmin = usuario?.rol === 'Administrador';

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

      {/* CONFIGURACION — al fondo, separado del resto */}
      <NavLink
        to="/configuracion"
        className={`nav-item clickable${isConfigActive ? ' active' : ''}`}
        style={{ marginTop: 'auto' }}
      >
        <svg className="nav-icon" viewBox="0 0 16 16" fill="none">
          <circle cx="8" cy="8" r="2.5" stroke="currentColor" strokeWidth="1.2" />
          <path
            d="M8 1v2M8 13v2M1 8h2M13 8h2M3.2 3.2l1.4 1.4M11.4 11.4l1.4 1.4M11.4 3.2l-1.4 1.4M4.6 11.4l-1.4 1.4"
            stroke="currentColor"
            strokeWidth="1.2"
            strokeLinecap="round"
          />
        </svg>
        Configuración
      </NavLink>
    </aside>
  );
}