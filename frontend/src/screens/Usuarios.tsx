// src/screens/Usuarios.tsx
// ============================================================================
//  Pantalla del ABM de usuarios.
//  Solo accesible para Administradores (protegido por ProtectedRoute).
//
//  Features:
//   - 4 cards resumen (total, admins, auditores, operadores)
//   - Buscador por nombre/email
//   - 4 filtros pill (Todos, Activos, Inactivos, por rol)
//   - Tabla con: nombre+avatar, email, rol, estado, acciones
//   - Acciones: Editar | Resetear pass | Activar/Desactivar
//   - Modal de crear (boton "+ Nuevo usuario")
//   - Modal de editar
//   - Modal de resetear contrasena
//   - Auto-bloqueo: botones deshabilitados si la fila es el admin logueado
// ============================================================================
import { useEffect, useMemo, useState } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { usuariosCss } from '../styles/usuarios';
import { useAuth } from '../login/AuthContext';
import {
  listarUsuarios,
  modificarUsuario,
  type UsuarioResponse,
  type Rol,
} from '../login/authApi';
import UsuarioModal from '../components/UsuarioModal';
import ResetPasswordModal from '../components/ResetPasswordModal';

type Filtro = 'Todos' | 'Activos' | 'Inactivos' | Rol;

export default function Usuarios() {
  useInjectStyle(usuariosCss, 'usuarios-style');

  const { usuario: adminActual } = useAuth();

  const [usuarios, setUsuarios] = useState<UsuarioResponse[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError]       = useState('');

  const [busqueda, setBusqueda] = useState('');
  const [filtro, setFiltro]     = useState<Filtro>('Todos');

  // Modales
  const [modalCrear, setModalCrear]                   = useState(false);
  const [modalEditar, setModalEditar]                 = useState<UsuarioResponse | null>(null);
  const [modalResetPass, setModalResetPass]           = useState<UsuarioResponse | null>(null);
  const [accionEnFila, setAccionEnFila]               = useState<number | null>(null);

  // ── Cargar listado ──────────────────────────────────────────────────────
  const recargarUsuarios = async () => {
    try {
      setCargando(true);
      setError('');
      const data = await listarUsuarios();
      setUsuarios(data);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al cargar usuarios.';
      setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setCargando(false);
    }
  };

  useEffect(() => {
    recargarUsuarios();
  }, []);

  // ── Stats ──────────────────────────────────────────────────────────────
  const stats = useMemo(() => {
    return {
      total:     usuarios.length,
      admins:    usuarios.filter((u) => u.rol === 'Administrador').length,
      auditores: usuarios.filter((u) => u.rol === 'Auditor').length,
      operadores:usuarios.filter((u) => u.rol === 'Operador').length,
    };
  }, [usuarios]);

  // ── Filtrado ───────────────────────────────────────────────────────────
  const usuariosFiltrados = useMemo(() => {
    let result = usuarios;

    if (filtro === 'Activos')    result = result.filter((u) => u.activo);
    else if (filtro === 'Inactivos') result = result.filter((u) => !u.activo);
    else if (filtro === 'Administrador' || filtro === 'Auditor' || filtro === 'Operador') {
      result = result.filter((u) => u.rol === filtro);
    }

    if (busqueda.trim()) {
      const q = busqueda.toLowerCase();
      result = result.filter(
        (u) =>
          u.nombre.toLowerCase().includes(q) ||
          u.email.toLowerCase().includes(q),
      );
    }

    return result;
  }, [usuarios, filtro, busqueda]);

  // ── Toggle activo/inactivo ─────────────────────────────────────────────
  const handleToggleActivo = async (u: UsuarioResponse) => {
    setAccionEnFila(u.id);
    try {
      const actualizado = await modificarUsuario(u.id, { activo: !u.activo });
      setUsuarios((prev) => prev.map((x) => (x.id === actualizado.id ? actualizado : x)));
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al actualizar.';
      alert(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setAccionEnFila(null);
    }
  };

  // ── Helper: clase de badge de rol ──────────────────────────────────────
  const claseRol = (rol: Rol): string => {
    if (rol === 'Administrador') return 'us-rol-admin';
    if (rol === 'Auditor')       return 'us-rol-auditor';
    return 'us-rol-operador';
  };

  // ── Helper: inicial del nombre ─────────────────────────────────────────
  const inicial = (nombre: string): string =>
    nombre.trim().charAt(0).toUpperCase() || '?';

  return (
    <>
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="us-header">
        <div>
          <h1 className="us-header-title">Usuarios</h1>
          <p className="us-header-sub">
            Gestión de cuentas del sistema — solo Administradores
          </p>
        </div>
        <button
          type="button"
          className="btn-pri"
          onClick={() => setModalCrear(true)}
        >
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
            <path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
          </svg>
          Nuevo usuario
        </button>
      </div>

      {/* ── Stats ───────────────────────────────────────────────────────── */}
      <div className="us-stats">
        <div className="us-stat-card">
          <div className="us-stat-label">Total usuarios</div>
          <div className="us-stat-value">{stats.total}</div>
        </div>
        <div className="us-stat-card">
          <div className="us-stat-label">Administradores</div>
          <div className="us-stat-value">{stats.admins}</div>
        </div>
        <div className="us-stat-card">
          <div className="us-stat-label">Auditores</div>
          <div className="us-stat-value">{stats.auditores}</div>
        </div>
        <div className="us-stat-card">
          <div className="us-stat-label">Operadores</div>
          <div className="us-stat-value">{stats.operadores}</div>
        </div>
      </div>

      {/* ── Filtros ─────────────────────────────────────────────────────── */}
      <div className="us-filters">
        <div className="us-search">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
            <circle cx="7" cy="7" r="5" stroke="currentColor" strokeWidth="1.4" />
            <path d="M11 11l3 3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <input
            type="text"
            placeholder="Buscar por nombre o email…"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
        </div>

        <div className="us-pills">
          <button
            type="button"
            className={`us-pill ${filtro === 'Todos' ? 'active' : ''}`}
            onClick={() => setFiltro('Todos')}
          >Todos</button>
          <button
            type="button"
            className={`us-pill ${filtro === 'Activos' ? 'active' : ''}`}
            onClick={() => setFiltro('Activos')}
          >Activos</button>
          <button
            type="button"
            className={`us-pill ${filtro === 'Inactivos' ? 'active' : ''}`}
            onClick={() => setFiltro('Inactivos')}
          >Inactivos</button>
          <button
            type="button"
            className={`us-pill ${filtro === 'Administrador' ? 'active' : ''}`}
            onClick={() => setFiltro('Administrador')}
          >Admins</button>
          <button
            type="button"
            className={`us-pill ${filtro === 'Auditor' ? 'active' : ''}`}
            onClick={() => setFiltro('Auditor')}
          >Auditores</button>
          <button
            type="button"
            className={`us-pill ${filtro === 'Operador' ? 'active' : ''}`}
            onClick={() => setFiltro('Operador')}
          >Operadores</button>
        </div>
      </div>

      {/* ── Tabla ───────────────────────────────────────────────────────── */}
      <div className="us-table-card">
        {cargando ? (
          <div className="us-empty">Cargando usuarios…</div>
        ) : error ? (
          <div className="us-empty" style={{ color: '#A32D2D' }}>{error}</div>
        ) : usuariosFiltrados.length === 0 ? (
          <div className="us-empty">No hay usuarios que coincidan con los filtros.</div>
        ) : (
          <table className="us-table">
            <thead>
              <tr>
                <th style={{ width: '35%' }}>Usuario</th>
                <th style={{ width: '15%' }}>Rol</th>
                <th style={{ width: '12%' }}>Estado</th>
                <th style={{ width: '18%' }}>Creado</th>
                <th style={{ width: '20%', textAlign: 'right' }}>Acciones</th>
              </tr>
            </thead>
            <tbody>
              {usuariosFiltrados.map((u) => {
                const esSiMismo = u.id === adminActual?.id;
                const procesando = accionEnFila === u.id;

                return (
                  <tr key={u.id}>
                    <td>
                      <div className="us-name-cell">
                        <span className="us-mini-avatar">{inicial(u.nombre)}</span>
                        <div>
                          <div className="us-row-name">
                            {u.nombre} {esSiMismo && <span style={{color:'#7b6aaa',fontWeight:400,fontSize:11}}>(vos)</span>}
                          </div>
                          <div className="us-row-email">{u.email}</div>
                        </div>
                      </div>
                    </td>
                    <td>
                      <span className={`us-rol-badge ${claseRol(u.rol)}`}>{u.rol}</span>
                    </td>
                    <td>
                      <span className={`us-estado ${u.activo ? 'activo' : 'inactivo'}`}>
                        {u.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td>
                      {new Date(u.fechaCreacion).toLocaleDateString('es-AR', {
                        day: '2-digit', month: '2-digit', year: 'numeric',
                      })}
                    </td>
                    <td>
                      <div className="us-actions">
                        <button
                          type="button"
                          className="us-action-btn"
                          onClick={() => setModalEditar(u)}
                          disabled={procesando}
                          title="Editar usuario"
                        >
                          Editar
                        </button>
                        <button
                          type="button"
                          className="us-action-btn"
                          onClick={() => setModalResetPass(u)}
                          disabled={procesando}
                          title="Restablecer contraseña"
                        >
                          Restablecer
                        </button>
                        <button
                          type="button"
                          className={`us-action-btn ${u.activo ? 'danger' : ''}`}
                          onClick={() => handleToggleActivo(u)}
                          disabled={procesando || esSiMismo}
                          title={esSiMismo ? 'No podés desactivarte a vos mismo' : (u.activo ? 'Desactivar' : 'Activar')}
                        >
                          {u.activo ? 'Desactivar' : 'Activar'}
                        </button>
                      </div>
                    </td>
                  </tr>
                );
              })}
            </tbody>
          </table>
        )}
      </div>

      {/* ── Modales ─────────────────────────────────────────────────────── */}
      {modalCrear && (
        <UsuarioModal
          mode="crear"
          onClose={() => setModalCrear(false)}
          onSaved={(nuevo) => {
            setUsuarios((prev) => [nuevo, ...prev]);
            setModalCrear(false);
          }}
        />
      )}

      {modalEditar && (
        <UsuarioModal
          mode="editar"
          usuario={modalEditar}
          onClose={() => setModalEditar(null)}
          onSaved={(actualizado) => {
            setUsuarios((prev) => prev.map((u) => (u.id === actualizado.id ? actualizado : u)));
            setModalEditar(null);
          }}
        />
      )}

      {modalResetPass && (
        <ResetPasswordModal
          usuario={modalResetPass}
          onClose={() => setModalResetPass(null)}
          onReset={() => setModalResetPass(null)}
        />
      )}
    </>
  );
}