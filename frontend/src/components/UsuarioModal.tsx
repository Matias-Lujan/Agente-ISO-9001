// src/components/UsuarioModal.tsx
// ============================================================================
//  Modal de crear/editar usuario.
//
//  Modos:
//   - mode="crear":   muestra todos los campos (nombre, email, password, rol)
//   - mode="editar":  muestra nombre, email, rol, activo (sin password)
//                     Password se cambia con el modal de reset.
//
//  Aplica reglas de auto-bloqueo en el frontend (ademas del backend):
//   - Si el usuario que se esta editando es el admin logueado:
//     - El switch "Activo" se deshabilita
//     - El select de rol se deshabilita
// ============================================================================
import { crearUsuario, modificarUsuario, type UsuarioResponse, type Rol } from '../login/authApi';
import { useEffect, useState, type FormEvent } from 'react';
import '../styles/usuarios'
import { useAuth } from '../login/AuthContext';

type Mode = 'crear' | 'editar';

interface Props {
  mode: Mode;
  usuario?: UsuarioResponse; // requerido en modo "editar"
  onClose: () => void;
  onSaved: (u: UsuarioResponse) => void;
}

export default function UsuarioModal({ mode, usuario, onClose, onSaved }: Props) {
  const { usuario: adminActual } = useAuth();

  // Estado del form
  const [nombre, setNombre]     = useState(usuario?.nombre ?? '');
  const [email, setEmail]       = useState(usuario?.email ?? '');
  const [password, setPassword] = useState('');
  const [verPass, setVerPass]   = useState(false);
  const [rol, setRol]           = useState<Rol>(usuario?.rol ?? 'Auditor');
  const [activo, setActivo]     = useState(usuario?.activo ?? true);

  const [enviando, setEnviando] = useState(false);
  const [error, setError]       = useState('');

  const esSiMismo = mode === 'editar' && usuario?.id === adminActual?.id;

  // Cerrar con ESC
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onClose]);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');

    // Validaciones del cliente
    const n = nombre.trim();
    if (!n) {
      setError('El nombre es obligatorio.');
      return;
    }

    const em = email.trim().toLowerCase();
    if (!em) {
      setError('El email es obligatorio.');
      return;
    }
    if (!em.endsWith('@bdtglobal.com.ar') && !em.endsWith('@bdtglobal.com')) {
      setError('Solo se permiten cuentas @bdtglobal.com.ar');
      return;
    }

    if (mode === 'crear') {
      if (!password || password.length < 8) {
        setError('La contraseña debe tener al menos 8 caracteres.');
        return;
      }
    }

    setEnviando(true);
    try {
      let resultado: UsuarioResponse;
      if (mode === 'crear') {
        resultado = await crearUsuario({
          nombre: n,
          email: em,
          password,
          rol,
        });
      } else {
        resultado = await modificarUsuario(usuario!.id, {
          nombre: n,
          email: em,
          rol: esSiMismo ? null : rol,         // si es uno mismo, no mandamos rol
          activo: esSiMismo ? null : activo,    // si es uno mismo, no mandamos activo
        });
      }
      onSaved(resultado);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al guardar.';
      setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setEnviando(false);
    }
  };

  const titulo = mode === 'crear' ? 'Crear usuario' : 'Editar usuario';
  const cta    = mode === 'crear' ? 'Crear' : 'Guardar cambios';

  return (
    <div className="us-modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="us-modal" onClick={(e) => e.stopPropagation()}>
        <div className="us-modal-header">
          <h2 className="us-modal-title">{titulo}</h2>
          <button
            type="button"
            className="us-modal-close"
            onClick={onClose}
            aria-label="Cerrar"
          >
            ×
          </button>
        </div>

        <form onSubmit={handleSubmit} noValidate>
          <div className="us-modal-body">
            {error && <div className="us-feedback error">{error}</div>}

            <div className="us-form-field">
              <label htmlFor="us-nombre">Nombre completo</label>
              <input
                id="us-nombre"
                type="text"
                value={nombre}
                onChange={(e) => setNombre(e.target.value)}
                placeholder="Ej: Matías Luján"
                disabled={enviando}
                autoFocus
              />
            </div>

            <div className="us-form-field">
              <label htmlFor="us-email">Correo electrónico</label>
              <input
                id="us-email"
                type="email"
                value={email}
                onChange={(e) => setEmail(e.target.value)}
                placeholder="usuario@bdtglobal.com.ar"
                disabled={enviando}
              />
              <div className="us-form-field-hint">
                Solo se permiten cuentas @bdtglobal.com.ar
              </div>
            </div>

            {mode === 'crear' && (
              <div className="us-form-field">
                <label htmlFor="us-pass">Contraseña</label>
                <div className="us-pass-wrap">
                  <input
                    id="us-pass"
                    type={verPass ? 'text' : 'password'}
                    value={password}
                    onChange={(e) => setPassword(e.target.value)}
                    placeholder="Mínimo 8 caracteres"
                    disabled={enviando}
                    autoComplete="new-password"
                  />
                  <button
                    type="button"
                    className="us-pass-toggle"
                    onClick={() => setVerPass(!verPass)}
                    aria-label={verPass ? 'Ocultar' : 'Mostrar'}
                    tabIndex={-1}
                  >
                    {verPass ? (
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
                        <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94"/>
                        <path d="M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19"/>
                        <line x1="1" y1="1" x2="23" y2="23"/>
                      </svg>
                    ) : (
                      <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
                        stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
                        <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
                        <circle cx="12" cy="12" r="3"/>
                      </svg>
                    )}
                  </button>
                </div>
              </div>
            )}

            <div className="us-form-field">
              <label htmlFor="us-rol">Rol</label>
              <select
                id="us-rol"
                value={rol}
                onChange={(e) => setRol(e.target.value as Rol)}
                disabled={enviando || esSiMismo}
              >
                <option value="Administrador">Administrador</option>
                <option value="Auditor">Auditor</option>
                <option value="Operador">Operador</option>
              </select>
              {esSiMismo && (
                <div className="us-form-field-hint">
                  No podés cambiarte el rol a vos mismo.
                </div>
              )}
            </div>

            {mode === 'editar' && (
              <div className="us-form-field">
                <label className="us-form-checkbox">
                  <input
                    type="checkbox"
                    checked={activo}
                    onChange={(e) => setActivo(e.target.checked)}
                    disabled={enviando || esSiMismo}
                  />
                  Usuario activo
                </label>
                {esSiMismo && (
                  <div className="us-form-field-hint">
                    No podés desactivar tu propia cuenta.
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="us-modal-footer">
            <button
              type="button"
              className="us-btn-sec"
              onClick={onClose}
              disabled={enviando}
            >
              Cancelar
            </button>
            <button type="submit" className="us-btn-pri" disabled={enviando}>
              {enviando && <span className="us-spinner" aria-hidden="true" />}
              {enviando ? 'Guardando…' : cta}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}