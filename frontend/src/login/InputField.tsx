
//  Campo de formulario reutilizable con label, manejo de errores y
//  toggle para mostrar/ocultar contrasena.
// ============================================================================
import React from 'react';
import { useState, type ChangeEvent, type FocusEvent } from 'react';

interface Props {
  id: string;
  name: string;
  label: string;
  type?: string;
  value: string;
  error?: string | null;
  disabled?: boolean;
  autoComplete?: string;
  onChange: (e: ChangeEvent<HTMLInputElement>) => void;
  onBlur?: (e: FocusEvent<HTMLInputElement>) => void;
}

export default function InputField({
  id,
  name,
  label,
  type = 'text',
  value,
  error,
  disabled = false,
  autoComplete,
  onChange,
  onBlur,
}: Props) {
  const [showPassword, setShowPassword] = useState(false);
  const isPassword   = type === 'password';
  const resolvedType = isPassword ? (showPassword ? 'text' : 'password') : type;

  return (
    <div className={`login-inputWrapper ${error ? 'login-hasError' : ''}`}>
      <label htmlFor={id} className="login-inputLabel">
        {label}
      </label>

      <div className="login-inputWrap">
        <input
          id={id}
          name={name}
          type={resolvedType}
          value={value}
          disabled={disabled}
          autoComplete={autoComplete}
          placeholder=" "
          onChange={onChange}
          onBlur={onBlur}
          aria-describedby={error ? `${id}-error` : undefined}
          aria-invalid={!!error}
          className="login-input"
        />

        {isPassword && (
          <button
            type="button"
            className="login-toggleBtn"
            onClick={() => setShowPassword((v) => !v)}
            aria-label={showPassword ? 'Ocultar contraseña' : 'Mostrar contraseña'}
            tabIndex={-1}
          >
            {showPassword ? <EyeOffIcon /> : <EyeIcon />}
          </button>
        )}
      </div>

      {error && (
        <span id={`${id}-error`} role="alert" className="login-fieldError">
          <ErrorIcon />
          {error}
        </span>
      )}
    </div>
  );
}

// ── Inline SVG icons ─────────────────────────────────────────────────────────

const EyeIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
    <path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/>
    <circle cx="12" cy="12" r="3"/>
  </svg>
);

const EyeOffIcon = () => (
  <svg width="16" height="16" viewBox="0 0 24 24" fill="none"
    stroke="currentColor" strokeWidth="1.8" strokeLinecap="round">
    <path d="M17.94 17.94A10.07 10.07 0 0112 20c-7 0-11-8-11-8a18.45 18.45 0 015.06-5.94"/>
    <path d="M9.9 4.24A9.12 9.12 0 0112 4c7 0 11 8 11 8a18.5 18.5 0 01-2.16 3.19"/>
    <line x1="1" y1="1" x2="23" y2="23"/>
  </svg>
);

const ErrorIcon = () => (
  <svg width="12" height="12" viewBox="0 0 24 24" fill="currentColor">
    <circle cx="12" cy="12" r="10"/>
    <line x1="12" y1="8" x2="12" y2="12" stroke="white" strokeWidth="2" strokeLinecap="round"/>
    <circle cx="12" cy="16" r="1" fill="white"/>
  </svg>
);