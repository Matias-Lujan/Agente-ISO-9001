// src/components/ResetPasswordModal.tsx
// ============================================================================
//  Modal para que el Administrador resetee la contrasena de otro usuario.
//  No pide la pass anterior (el admin no la conoce).
// ============================================================================
import React from 'react';
import { useEffect, useState, type FormEvent } from 'react';
import { resetearPassword, type UsuarioResponse } from '../login/authApi';

interface Props {
  usuario: UsuarioResponse;
  onClose: () => void;
  onReset: () => void;
}

export default function ResetPasswordModal({ usuario, onClose, onReset }: Props) {
  const [pass1, setPass1]       = useState('');
  const [pass2, setPass2]       = useState('');
  const [verPass, setVerPass]   = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError]       = useState('');
  const [exito, setExito]       = useState(false);

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

    if (!pass1 || pass1.length < 8) {
      setError('La contraseña debe tener al menos 8 caracteres.');
      return;
    }
    if (pass1 !== pass2) {
      setError('Las contraseñas no coinciden.');
      return;
    }

    setEnviando(true);
    try {
      await resetearPassword(usuario.id, { passwordNueva: pass1 });
      setExito(true);
      // Esperar 1 seg para que el usuario vea el feedback antes de cerrar
      setTimeout(() => {
        onReset();
      }, 900);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al resetear.';
      setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="us-modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="us-modal" onClick={(e) => e.stopPropagation()}>
        <div className="us-modal-header">
          <h2 className="us-modal-title">Resetear contraseña</h2>
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
            <p style={{ fontSize: 13, color: '#5a4a8a', marginBottom: '1rem' }}>
              Vas a resetear la contraseña de <strong>{usuario.nombre}</strong> ({usuario.email}).
              La próxima vez que ingrese, deberá usar esta nueva contraseña.
            </p>

            {error && <div className="us-feedback error">{error}</div>}
            {exito && <div className="us-feedback success">Contraseña actualizada correctamente.</div>}

            <div className="us-form-field">
              <label htmlFor="rp-pass1">Nueva contraseña</label>
              <div className="us-pass-wrap">
                <input
                  id="rp-pass1"
                  type={verPass ? 'text' : 'password'}
                  value={pass1}
                  onChange={(e) => setPass1(e.target.value)}
                  placeholder="Mínimo 8 caracteres"
                  disabled={enviando || exito}
                  autoFocus
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

            <div className="us-form-field">
              <label htmlFor="rp-pass2">Confirmar nueva contraseña</label>
              <input
                id="rp-pass2"
                type={verPass ? 'text' : 'password'}
                value={pass2}
                onChange={(e) => setPass2(e.target.value)}
                placeholder="Repetir contraseña"
                disabled={enviando || exito}
                autoComplete="new-password"
              />
            </div>
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
            <button type="submit" className="us-btn-pri" disabled={enviando || exito}>
              {enviando && <span className="us-spinner" aria-hidden="true" />}
              {enviando ? 'Reseteando…' : 'Resetear contraseña'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}