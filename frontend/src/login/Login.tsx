// src/login/Login.tsx
// ============================================================================
//  Pantalla de login con split-screen animado (signin / signup).
//
//  Diseno original adaptado a TypeScript estricto y conectado al AuthContext
//  existente. Mantiene:
//   - Split-screen con animacion de transicion entre vistas
//   - Fondo animado de red de nodos (Canvas)
//   - Panel violeta con gradiente
//   - Tipografias Syne (display) + DM Sans (body)
//   - Sistema de tokens CSS
//
//  La logica del login (validacion, llamado al backend, JWT) sigue viviendo
//  en AuthContext — esta pantalla solo dispara iniciarSesion() y reacciona.
// ============================================================================
import { useEffect, useState, type ChangeEvent, type FormEvent } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { useAuth } from './AuthContext';
import { rutaInicial } from '../utils/navegacion';
import { loginCss } from './loginStyles';
import NetworkBackground, { type NetworkConfig } from './NetworkBackground';
import InputField from './InputField.tsx';

// Tipo del state pasado por react-router cuando redirige al login
interface LocationState {
  from?: { pathname: string };
}

// ── Configs del fondo animado ────────────────────────────────────────────────
const BG_CONFIG: NetworkConfig = {
  nodeCount: 55,
  nodeColor: 'rgba(75,45,171,0.55)',
  lineColor: 'rgba(75,45,171,0.18)',
  nodeSpeed: 0.25,
};

const MINI_CONFIG: NetworkConfig = {
  nodeCount: 35,
  nodeColor: 'rgba(255,255,255,0.35)',
  lineColor: 'rgba(255,255,255,0.18)',
  nodeSpeed: 0.2,
};

// ── Logo BDT ─────────────────────────────────────────────────────────────────
function BdtLogo() {
  return (
    <div className="login-bdtLogo">
      <svg width="32" height="32" viewBox="0 0 38 38" fill="none">
        <defs>
          <radialGradient id="bdtg" cx="35%" cy="30%" r="70%">
            <stop offset="0%"   stopColor="rgba(255,255,255,0.9)"/>
            <stop offset="100%" stopColor="rgba(255,255,255,0.3)"/>
          </radialGradient>
        </defs>
        <circle cx="19" cy="19" r="17" fill="url(#bdtg)"/>
        <path d="M10 19 Q14 10 19 19 Q24 28 28 19" stroke="rgba(75,45,171,0.7)" strokeWidth="1" fill="none"/>
        <ellipse cx="19" cy="19" rx="17" ry="9" stroke="rgba(75,45,171,0.5)" strokeWidth="1" fill="none"/>
        <path d="M8 13 Q19 8 30 13"  stroke="rgba(75,45,171,0.4)" strokeWidth="0.8" fill="none"/>
        <path d="M8 25 Q19 30 30 25" stroke="rgba(75,45,171,0.4)" strokeWidth="0.8" fill="none"/>
      </svg>
      <span className="login-bdtWordmark">
        <span>bdt</span>global
      </span>
    </div>
  );
}

// ── Panel formulario signin ──────────────────────────────────────────────────
interface SignInFormProps {
  onSuccess: () => void;
}

function SignInForm({ onSuccess }: SignInFormProps) {
  const { iniciarSesion, cargando, error: serverError } = useAuth();

  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [errors, setErrors] = useState<{ email: string | null; password: string | null }>({
    email: null,
    password: null,
  });
  const [touched, setTouched] = useState<{ email: boolean; password: boolean }>({
    email: false,
    password: false,
  });

  // Validacion local (UX rapida — la seguridad real la hace el backend)
  const validate = (vals: { email: string; password: string }) => {
    const errs: { email: string | null; password: string | null } = { email: null, password: null };

    if (!vals.email.trim()) {
      errs.email = 'El email es requerido.';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(vals.email)) {
      errs.email = 'Ingresá un email válido.';
    } else {
      const lower = vals.email.toLowerCase();
      if (!lower.endsWith('@bdtglobal.com.ar') && !lower.endsWith('@bdtglobal.com')) {
        errs.email = 'Solo se permiten cuentas @bdtglobal.com.ar';
      }
    }

    if (!vals.password) {
      errs.password = 'La contraseña es requerida.';
    }

    return errs;
  };

  const handleEmailChange = (e: ChangeEvent<HTMLInputElement>) => {
    setEmail(e.target.value);
    if (touched.email) {
      setErrors((prev) => ({ ...prev, email: validate({ email: e.target.value, password }).email }));
    }
  };

  const handlePasswordChange = (e: ChangeEvent<HTMLInputElement>) => {
    setPassword(e.target.value);
    if (touched.password) {
      setErrors((prev) => ({ ...prev, password: validate({ email, password: e.target.value }).password }));
    }
  };

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setTouched({ email: true, password: true });

    const newErrors = validate({ email, password });
    setErrors(newErrors);
    if (newErrors.email || newErrors.password) return;

    const resultado = await iniciarSesion(email.trim(), password);
    if (resultado.ok) {
      onSuccess();
    }
  };

  return (
    <form onSubmit={handleSubmit} className="login-form" noValidate aria-label="Formulario de acceso">
      <h2 className="login-form-title">Bienvenido</h2>
      <p className="login-form-subtitle">Ingresá tus credenciales para continuar</p>

      {serverError && (
        <div role="alert" className="login-alert">
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="2" strokeLinecap="round">
            <circle cx="12" cy="12" r="10"/>
            <line x1="12" y1="8" x2="12" y2="12"/>
            <line x1="12" y1="16" x2="12.01" y2="16"/>
          </svg>
          <span>{serverError}</span>
        </div>
      )}

      <div className="login-fields">
        <InputField
          id="email"
          name="email"
          label="Correo electrónico"
          type="email"
          value={email}
          error={errors.email}
          disabled={cargando}
          autoComplete="username"
          onChange={handleEmailChange}
          onBlur={() => {
            setTouched((p) => ({ ...p, email: true }));
            setErrors((p) => ({ ...p, email: validate({ email, password }).email }));
          }}
        />

        <InputField
          id="password"
          name="password"
          label="Contraseña"
          type="password"
          value={password}
          error={errors.password}
          disabled={cargando}
          autoComplete="current-password"
          onChange={handlePasswordChange}
          onBlur={() => {
            setTouched((p) => ({ ...p, password: true }));
            setErrors((p) => ({ ...p, password: validate({ email, password }).password }));
          }}
        />
      </div>

      <div className="login-forgotWrap">
        <button type="button" className="login-forgotLink">
          ¿Olvidaste tu contraseña?
        </button>
      </div>

      <button type="submit" className="login-btn-primary" disabled={cargando}>
        {cargando ? <span className="login-spinner" aria-hidden="true" /> : null}
        <span>{cargando ? '' : 'Iniciar sesión'}</span>
      </button>

      <p className="login-footer">
        ¿Problemas para acceder?{' '}
        <a href="mailto:soporte@bdtglobal.com" className="login-link">
          Contactá a soporte
        </a>
      </p>
    </form>
  );
}

// ── Panel formulario signup (placeholder) ────────────────────────────────────
function SignUpForm({ onSwitchToSignIn }: { onSwitchToSignIn: () => void }) {
  return (
    <>
      <h2 className="login-formTitle">Crear cuenta</h2>
      <p className="login-formSub">Completá tus datos para registrarte</p>
      <p style={{ fontSize: 13, color: 'var(--color-login-text-muted)', marginTop: '1rem' }}>
        Funcionalidad en desarrollo.
      </p>
      <p className="login-switchText">
        ¿Ya tenés cuenta?{' '}
        <button className="login-switchBtn" onClick={onSwitchToSignIn} type="button">
          Iniciar sesión
        </button>
      </p>
    </>
  );
}

// ── Panel brand (violeta) ────────────────────────────────────────────────────
function BrandPanel({ onSwitch }: { onSwitch: (mode: 'signin' | 'signup') => void }) {
  return (
    <div className="login-brandSide">
      <NetworkBackground config={MINI_CONFIG} />

      <div className="login-brandContent">
        {/* Vista signin: invita a registrarse */}
        <div className="login-brandPanel login-brandSignin">
          <BdtLogo />
          <h1 className="login-brandTitle">
            Auditorías ISO 9001
            <br />
            impulsadas por IA
          </h1>
          <p className="login-brandSub">
            Detecta no conformidades, clasifica hallazgos y genera informes en minutos, no en semanas.
          </p>
          <div className="login-brandStat">
            <span className="login-statVal">100%</span>
            <span className="login-statLbl">Trazable</span>
          </div>
          <button className="login-brandBtn" onClick={() => onSwitch('signup')} type="button">
            Crear cuenta
          </button>
        </div>

        {/* Vista signup: invita a iniciar sesion */}
        <div className="login-brandPanel login-brandSignup">
          <h1 className="login-brandTitle">¡Bienvenido de vuelta!</h1>
          <p className="login-brandSub">
            Ingresá con tus credenciales para acceder al sistema de auditoría ISO 9001.
          </p>
          <button className="login-brandBtn" onClick={() => onSwitch('signin')} type="button">
            Iniciar sesión
          </button>
        </div>
      </div>
    </div>
  );
}

// ── Login (page) ─────────────────────────────────────────────────────────────
export default function Login() {
  useInjectStyle(loginCss, 'login-design-style');

  const { estaAutenticado, usuario } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [mode, setMode] = useState<'signin' | 'signup'>('signin');

  // Si ya esta logueado, redirigir al destino guardado o a la pantalla inicial
  // que corresponde al rol (Operador → /hallazgos, resto → /dashboard).
  useEffect(() => {
    if (estaAutenticado) {
      const state = location.state as LocationState | null;
      const destino = state?.from?.pathname ?? rutaInicial(usuario?.rol);
      navigate(destino, { replace: true });
    }
  }, [estaAutenticado, usuario, navigate, location]);

  const handleLoginSuccess = () => {
    const state = location.state as LocationState | null;
    const destino = state?.from?.pathname ?? rutaInicial(usuario?.rol);
    navigate(destino, { replace: true });
  };

  return (
    <div className="login-page-root">
      <main className="login-page">
        {/* Fondo animado */}
        <NetworkBackground config={BG_CONFIG} />

        <div className={`login-container ${mode === 'signup' ? 'login-signupMode' : ''}`}>
          {/* Panel formulario (lado blanco) */}
          <div className="login-formSide">
            {/* Sign In */}
            <div className="login-formPanel login-signInPanel">
              <SignInForm onSuccess={handleLoginSuccess} />
            </div>

            {/* Sign Up */}
            <div className="login-formPanel login-signUpPanel">
              <SignUpForm onSwitchToSignIn={() => setMode('signin')} />
            </div>
          </div>

          {/* Panel violeta */}
          <BrandPanel onSwitch={setMode} />
        </div>
      </main>
    </div>
  );
}