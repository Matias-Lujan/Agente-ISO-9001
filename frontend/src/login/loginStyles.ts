// src/login/loginStyles.ts
// ============================================================================
//  Estilos del Login.
//
//  Sigue el patron del proyecto: CSS como string, inyectado una sola vez
//  con useInjectStyle al montar la pantalla. Misma paleta que sharedCss
//  (#1e1050 / #4B2DAB / #f0eefa / #faf8ff / #7B52E8) para que el login se
//  sienta parte del mismo producto.
// ============================================================================

export const loginCss = `
/* Pantalla de login — ocupa el viewport entero, sin sidebar */
.login-page{
  min-height:100vh;
  display:flex;
  align-items:center;
  justify-content:center;
  background:#f0eefa;
  padding:1rem;
  font-family:-apple-system,BlinkMacSystemFont,"Segoe UI",sans-serif;
}

.login-card{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.15);
  border-radius:14px;
  padding:2.5rem 2rem;
  width:100%;
  max-width:400px;
  box-shadow:0 8px 40px rgba(30,16,80,0.08);
}

.login-brand{
  display:flex;
  align-items:center;
  gap:10px;
  margin-bottom:1.75rem;
}
.login-brand img{height:24px;width:auto;}

.login-title{
  font-size:22px;
  font-weight:500;
  color:#1e1050;
  margin:0 0 4px;
}

.login-sub{
  font-size:13px;
  color:#7b6aaa;
  margin:0 0 1.75rem;
}
.login-sub strong{color:#4B2DAB;font-weight:500;}

.login-form{
  display:flex;
  flex-direction:column;
  gap:1.1rem;
}

.login-field{
  display:flex;
  flex-direction:column;
  gap:6px;
}

.login-field label{
  font-size:12px;
  font-weight:500;
  color:#5a4a8a;
}

.login-input{
  background:#faf8ff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:8px;
  padding:10px 12px;
  font-size:13px;
  color:#1e1050;
  outline:none;
  transition:all .2s;
  width:100%;
  font-family:inherit;
}
.login-input::placeholder{color:#b5a8d3;}
.login-input:focus{border-color:#7B52E8;background:#fff;}
.login-input.error{border-color:#A32D2D;}
.login-input:disabled{opacity:0.5;cursor:not-allowed;}

.login-input-wrapper{
  position:relative;
  display:flex;
  align-items:center;
}
.login-input-wrapper .login-input{padding-right:40px;}

.login-toggle-pass{
  position:absolute;
  right:10px;
  background:none;
  border:none;
  cursor:pointer;
  font-size:16px;
  color:#7b6aaa;
  padding:0;
  line-height:1;
}
.login-toggle-pass:hover{color:#4B2DAB;}

.login-error{
  background:#FCEBEB;
  color:#A32D2D;
  padding:10px 12px;
  border-radius:8px;
  font-size:13px;
  display:flex;
  align-items:center;
  gap:8px;
}

.login-btn{
  background:#4B2DAB;
  color:#fff;
  border:none;
  border-radius:8px;
  padding:11px;
  font-size:14px;
  font-weight:500;
  cursor:pointer;
  transition:background .2s;
  display:flex;
  align-items:center;
  justify-content:center;
  min-height:42px;
  margin-top:4px;
  font-family:inherit;
}
.login-btn:hover:not(:disabled){background:#3a2090;}
.login-btn:disabled{opacity:0.5;cursor:not-allowed;}

.login-spinner{
  width:18px;
  height:18px;
  border:2px solid rgba(255,255,255,0.3);
  border-top-color:#fff;
  border-radius:50%;
  animation:login-spin 0.7s linear infinite;
}
@keyframes login-spin{to{transform:rotate(360deg);}}
`;