// src/login/loginStyles.ts
// ============================================================================
//  Estilos del Login.
//
//  Decision tecnica: usamos CSS-in-string + useInjectStyle (igual patron que
//  el resto del proyecto en styles/shared.ts). Asi evitamos meter CSS Modules
//  como dependencia nueva. Las clases viven en el global namespace pero
//  prefijadas con `login-` para evitar choques.
//
//  Mantiene 100% del diseno original (split-screen, animacion, tipografias,
//  fondo animado, panel violeta, etc).
// ============================================================================

export const loginCss = `
/* ────────────────────────────────────────────────────────────────────────── */
/*  TOKENS                                                                    */
/* ────────────────────────────────────────────────────────────────────────── */

@import url('https://fonts.googleapis.com/css2?family=DM+Sans:wght@300;400;500;600&family=Syne:wght@700;800&display=swap');

.login-page-root {
  /* Brand palette */
  --color-brand-700:  #1e1050;
  --color-brand-500:  #4B2DAB;
  --color-brand-400:  #6b4fd4;
  --color-brand-300:  #9d88e8;
  --color-brand-200:  #c4b5f8;
  --color-brand-100:  #e2dcf8;
  --color-brand-50:   #f0eefa;

  /* Login especificos */
  --color-login-bg:           var(--color-brand-50);
  --color-login-card:         #ffffff;
  --color-login-text:         var(--color-brand-700);
  --color-login-text-muted:   #7b6aaa;
  --color-login-text-light:   #9080bb;
  --color-login-accent:       #7B52E8;
  --color-login-input-bg:     rgba(240, 235, 255, 0.7);
  --color-login-input-border: rgba(120, 80, 200, 0.25);
  --color-login-input-color:  var(--color-brand-700);
  --color-login-error:        #A32D2D;
  --color-login-error-bg:     rgba(252, 235, 235, 0.6);
  --color-login-error-border: rgba(163, 45, 45, 0.5);
  --color-login-shadow:       rgba(75, 45, 171, 0.18);
  --color-brand-gradient:     linear-gradient(135deg, #3a1e8f 0%, #4B2DAB 50%, #6b4fd4 100%);

  --font-display: 'Syne', sans-serif;
  --font-body:    'DM Sans', sans-serif;

  --space-1:  4px;
  --space-2:  8px;
  --space-3:  12px;
  --space-4:  16px;
  --space-5:  20px;
  --space-6:  24px;
  --space-8:  32px;

  --radius-sm:   6px;
  --radius-md:   10px;
  --radius-lg:   16px;
  --radius-xl:   24px;

  --transition-fast:   150ms ease;
  --transition-normal: 250ms ease;
  --transition-slow:   400ms ease;
  --transition-panel:  0.6s cubic-bezier(0.77, 0, 0.18, 1);

  font-family: var(--font-body);
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  PAGINA + CONTENEDOR                                                       */
/* ────────────────────────────────────────────────────────────────────────── */

.login-page {
  position: relative;
  width: 100%;
  height: 100vh;
  background: var(--color-login-bg);
  display: flex;
  align-items: center;
  justify-content: center;
  overflow: hidden;
}

.login-container {
  position: relative;
  z-index: 2;
  display: flex;
  width: min(900px, 94vw);
  min-height: 540px;
  border-radius: var(--radius-xl);
  overflow: hidden;
  box-shadow: 0 24px 60px var(--color-login-shadow);
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  PANEL FORMULARIO (lado blanco)                                            */
/* ────────────────────────────────────────────────────────────────────────── */

.login-formSide {
  position: relative;
  flex: 1;
  background: var(--color-login-card);
  overflow: hidden;
  min-width: 0;
}

.login-formPanel {
  position: absolute;
  inset: 0;
  padding: 3rem 2.5rem;
  display: flex;
  flex-direction: column;
  justify-content: center;
  transition: transform var(--transition-panel), opacity var(--transition-slow);
}

/* Sign In visible por defecto */
.login-signInPanel { transform: translateX(0);    opacity: 1; pointer-events: all; }
.login-signUpPanel { transform: translateX(100%); opacity: 0; pointer-events: none; }

/* Cuando cambia a signup */
.login-signupMode .login-signInPanel { transform: translateX(-100%); opacity: 0; pointer-events: none; }
.login-signupMode .login-signUpPanel { transform: translateX(0);     opacity: 1; pointer-events: all; }

.login-formTitle {
  font-size: 26px;
  font-weight: 600;
  color: var(--color-login-text);
  margin: 0 0 4px;
}

.login-formSub {
  font-size: 14px;
  color: var(--color-login-text-muted);
  margin: 0 0 1.75rem;
}

.login-switchText {
  text-align: center;
  font-size: 13px;
  color: var(--color-login-text-light);
  margin-top: 1.25rem;
}

.login-switchBtn {
  background: none;
  border: none;
  cursor: pointer;
  font-family: var(--font-body);
  font-size: 13px;
  font-weight: 600;
  color: var(--color-brand-500);
  padding: 0;
  text-decoration: underline;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  PANEL VIOLETA (brand)                                                     */
/* ────────────────────────────────────────────────────────────────────────── */

.login-brandSide {
  position: relative;
  width: 340px;
  flex-shrink: 0;
  background: var(--color-brand-gradient);
  overflow: hidden;
  transition: transform var(--transition-panel);
}

.login-container:not(.login-signupMode) .login-brandSide { order: 2; }
.login-signupMode .login-brandSide                       { order: 0; }

.login-brandContent {
  position: relative;
  z-index: 1;
  height: 100%;
  overflow: hidden;
}

.login-brandPanel {
  position: absolute;
  inset: 0;
  padding: 3rem 2rem;
  display: flex;
  flex-direction: column;
  justify-content: center;
  gap: 1rem;
  transition: transform var(--transition-panel), opacity var(--transition-slow);
}

.login-brandSignin { transform: translateX(0);    opacity: 1; pointer-events: all; }
.login-brandSignup { transform: translateX(100%); opacity: 0; pointer-events: none; }

.login-signupMode .login-brandSignin { transform: translateX(-100%); opacity: 0; pointer-events: none; }
.login-signupMode .login-brandSignup { transform: translateX(0);     opacity: 1; pointer-events: all; }

/* ── Logo BDT ────────────────────────────────────────────────────────────── */

.login-bdtLogo {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  margin-bottom: 0.25rem;
}

.login-bdtWordmark {
  font-size: 18px;
  font-weight: 800;
  color: #fff;
  letter-spacing: -0.5px;
  font-family: var(--font-display);
}

.login-bdtWordmark span {
  font-weight: 400;
  opacity: 0.85;
}

/* ── Contenido brand ─────────────────────────────────────────────────────── */

.login-brandTitle {
  font-family: var(--font-display);
  font-size: clamp(22px, 2.8vw, 28px);
  font-weight: 800;
  color: #fff;
  line-height: 1.2;
  letter-spacing: -0.02em;
  margin: 0;
}

.login-brandSub {
  font-size: 14px;
  color: rgba(255, 255, 255, 0.72);
  line-height: 1.65;
}

.login-brandStat {
  display: flex;
  align-items: baseline;
  gap: var(--space-2);
}

.login-statVal {
  font-family: var(--font-display);
  font-size: 32px;
  font-weight: 800;
  color: #fff;
  line-height: 1;
}

.login-statLbl {
  font-size: 11px;
  font-weight: 600;
  color: rgba(255, 255, 255, 0.6);
  text-transform: uppercase;
  letter-spacing: 0.08em;
}

.login-brandBtn {
  margin-top: 0.5rem;
  align-self: flex-start;
  background: transparent;
  border: 1.5px solid rgba(255, 255, 255, 0.7);
  border-radius: var(--radius-md);
  padding: 10px 24px;
  font-family: var(--font-body);
  font-size: 14px;
  font-weight: 600;
  color: #fff;
  cursor: pointer;
  transition: background var(--transition-fast), border-color var(--transition-fast);
}

.login-brandBtn:hover {
  background: rgba(255, 255, 255, 0.15);
  border-color: #fff;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  FORMULARIO                                                                */
/* ────────────────────────────────────────────────────────────────────────── */

.login-form {
  display: flex;
  flex-direction: column;
  width: 100%;
}

.login-form-title {
  font-size: 24px;
  font-weight: 600;
  color: var(--color-login-text);
  margin: 0 0 0.25rem;
}

.login-form-subtitle {
  font-size: 14px;
  color: var(--color-login-text-muted);
  margin: 0 0 1.75rem;
}

.login-fields {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.login-forgotWrap {
  display: flex;
  justify-content: flex-end;
  margin: 0.75rem 0 1.75rem;
}

.login-forgotLink {
  background: none;
  border: none;
  cursor: pointer;
  font-family: var(--font-body);
  font-size: 13px;
  color: var(--color-login-accent);
  padding: 0;
  transition: opacity var(--transition-fast);
}

.login-forgotLink:hover { opacity: 0.75; }

.login-footer {
  text-align: center;
  font-size: 13px;
  color: var(--color-login-text-light);
  margin-top: 1.25rem;
}

.login-link {
  color: var(--color-login-accent);
  text-decoration: none;
  transition: opacity var(--transition-fast);
}

.login-link:hover {
  opacity: 0.75;
  text-decoration: underline;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  INPUT FIELD                                                               */
/* ────────────────────────────────────────────────────────────────────────── */

.login-inputWrapper {
  position: relative;
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.login-inputLabel {
  font-size: 13px;
  color: var(--color-login-text-muted);
}

.login-inputWrap {
  position: relative;
  width: 100%;
}

.login-input {
  width: 100%;
  height: 44px;
  padding: 0 40px 0 14px;
  background: var(--color-login-input-bg);
  border: 0.5px solid var(--color-login-input-border);
  border-radius: var(--radius-md);
  color: var(--color-login-input-color);
  font-family: var(--font-body);
  font-size: 14px;
  outline: none;
  transition: border-color var(--transition-fast), background var(--transition-fast);
}

.login-input::placeholder { color: var(--color-brand-200); }

.login-input:hover:not(:disabled) {
  border-color: rgba(120, 80, 200, 0.4);
}

.login-input:focus {
  border-color: var(--color-login-accent);
  background: rgba(240, 235, 255, 0.95);
}

.login-input:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.login-toggleBtn {
  position: absolute;
  right: 10px;
  top: 50%;
  transform: translateY(-50%);
  background: none;
  border: none;
  cursor: pointer;
  color: var(--color-login-text-light);
  padding: 2px;
  display: flex;
  align-items: center;
  transition: color var(--transition-fast);
}

.login-toggleBtn:hover { color: var(--color-brand-500); }

.login-hasError .login-input,
.login-hasError .login-input:hover,
.login-hasError .login-input:focus {
  border-color: var(--color-login-error-border);
  background:   var(--color-login-error-bg);
}

.login-fieldError {
  display: flex;
  align-items: center;
  gap: 4px;
  font-size: 12px;
  color: var(--color-login-error);
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  ALERT BANNER                                                              */
/* ────────────────────────────────────────────────────────────────────────── */

.login-alert {
  display: flex;
  align-items: center;
  gap: var(--space-2);
  padding: var(--space-3) var(--space-4);
  border-radius: var(--radius-md);
  font-size: 13px;
  font-weight: 400;
  margin-bottom: var(--space-4);
  animation: login-slideIn 0.2s ease;
  background: var(--color-login-error-bg);
  border: 1px solid rgba(240, 149, 149, 0.2);
  color: var(--color-login-error);
}

@keyframes login-slideIn {
  from { opacity: 0; transform: translateY(-4px); }
  to   { opacity: 1; transform: translateY(0); }
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  BOTON PRINCIPAL                                                           */
/* ────────────────────────────────────────────────────────────────────────── */

.login-btn-primary {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: var(--space-2);
  width: 100%;
  height: 52px;
  padding: 0 var(--space-8);
  font-size: 15px;
  border: none;
  border-radius: var(--radius-md);
  font-family: var(--font-body);
  font-weight: 500;
  background: var(--color-brand-500);
  color: #fff;
  cursor: pointer;
  transition:
    background  var(--transition-fast),
    opacity     var(--transition-fast),
    transform   var(--transition-fast);
  white-space: nowrap;
}

.login-btn-primary:not(:disabled):hover {
  background: var(--color-brand-400);
}

.login-btn-primary:not(:disabled):active {
  transform: scale(0.98);
}

.login-btn-primary:disabled {
  cursor: not-allowed;
  opacity: 0.5;
}

.login-spinner {
  width: 16px;
  height: 16px;
  border: 2px solid rgba(255, 255, 255, 0.3);
  border-top-color: #fff;
  border-radius: 50%;
  animation: login-spin 0.7s linear infinite;
  flex-shrink: 0;
}

@keyframes login-spin {
  to { transform: rotate(360deg); }
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  RESPONSIVE                                                                */
/* ────────────────────────────────────────────────────────────────────────── */

@media (max-width: 700px) {
  .login-container { flex-direction: column; width: 94vw; min-height: unset; }
  .login-brandSide { width: 100%; min-height: 200px; order: 0 !important; }
  .login-formPanel { position: relative; padding: 2rem 1.5rem; }
  .login-formSide  { min-height: 420px; }
  .login-signInPanel, .login-signUpPanel { transition: none; }
}
`;