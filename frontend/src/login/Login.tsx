// src/login/Login.tsx
// ============================================================================
//  Pantalla de login. Sigue las convenciones del proyecto:
//   - TypeScript con strict mode (sin parametros no usados)
//   - useInjectStyle para inyectar el CSS una sola vez
//   - sin libs adicionales (solo react + react-router-dom)
// ============================================================================

import { useEffect, useState, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { useAuth } from './AuthContext';
import { loginCss } from './loginStyles';

// Tipo del state pasado por react-router cuando redirige al login
interface LocationState {
  from?: { pathname: string };
}

export default function Login() {
  useInjectStyle(loginCss, 'login-style');

  const { estaAutenticado, iniciarSesion, cargando, error } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [verPass, setVerPass] = useState(false);
  const [errLocal, setErrLocal] = useState('');

  // Si ya esta logueado, redirigir al destino guardado o al home
  useEffect(() => {
    if (estaAutenticado) {
      const state = location.state as LocationState | null;
      const destino = state?.from?.pathname ?? '/nueva-auditoria';
      navigate(destino, { replace: true });
    }
  }, [estaAutenticado, navigate, location]);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setErrLocal('');

    // Validacion en el frontend — UX rapida.
    // La seguridad real la hace el backend.
    if (!email.trim()) {
      setErrLocal('Ingresá tu email.');
      return;
    }

    const emailLower = email.toLowerCase();
    if (
      !emailLower.endsWith('@bdtglobal.com.ar') &&
      !emailLower.endsWith('@bdtglobal.com')
    ) {
      setErrLocal('Solo se permiten cuentas @bdtglobal.com.ar');
      return;
    }

    if (!password) {
      setErrLocal('Ingresá tu contraseña.');
      return;
    }

    const resultado = await iniciarSesion(email.trim(), password);

    if (resultado.ok) {
      const state = location.state as LocationState | null;
      const destino = state?.from?.pathname ?? '/nueva-auditoria';
      navigate(destino, { replace: true });
    }
  };

  const mensajeError = errLocal || error;

  return (
    <div className="login-page">
      <div className="login-card">
        <div className="login-brand">
          <img
            src="https://bdtglobal.com/img/logo15horizontal-min.png"
            alt="bdtglobal"
            style={{ filter: 'brightness(0) saturate(100%) invert(11%) sepia(56%) saturate(2500%) hue-rotate(245deg)' }}
          />
        </div>

        <h1 className="login-title">Iniciar sesión</h1>
        <p className="login-sub">
          Solo cuentas <strong>@bdtglobal.com.ar</strong>
        </p>

        <form className="login-form" onSubmit={handleSubmit} noValidate>
          <div className="login-field">
            <label htmlFor="email">Email</label>
            <input
              id="email"
              type="email"
              autoComplete="email"
              placeholder="usuario@bdtglobal.com.ar"
              value={email}
              onChange={(e) => {
                setEmail(e.target.value);
                setErrLocal('');
              }}
              disabled={cargando}
              className={`login-input${mensajeError ? ' error' : ''}`}
            />
          </div>

          <div className="login-field">
            <label htmlFor="password">Contraseña</label>
            <div className="login-input-wrapper">
              <input
                id="password"
                type={verPass ? 'text' : 'password'}
                autoComplete="current-password"
                placeholder="••••••••"
                value={password}
                onChange={(e) => {
                  setPassword(e.target.value);
                  setErrLocal('');
                }}
                disabled={cargando}
                className={`login-input${mensajeError ? ' error' : ''}`}
              />
              <button
                type="button"
                className="login-toggle-pass"
                onClick={() => setVerPass((v) => !v)}
                tabIndex={-1}
                aria-label={verPass ? 'Ocultar contraseña' : 'Mostrar contraseña'}
              >
                {verPass ? '🙈' : '👁️'}
              </button>
            </div>
          </div>

          {mensajeError && (
            <div className="login-error" role="alert">
              <span>⚠</span> {mensajeError}
            </div>
          )}

          <button type="submit" className="login-btn" disabled={cargando}>
            {cargando ? <span className="login-spinner" aria-label="Cargando…" /> : 'Ingresar'}
          </button>
        </form>
      </div>
    </div>
  );
}