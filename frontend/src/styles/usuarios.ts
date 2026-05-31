// src/styles/usuarios.ts
// ============================================================================
//  Estilos de la pantalla Usuarios (ABM).
//  Sigue la paleta del resto (1e1050 / 4B2DAB / f0eefa / faf8ff / 7B52E8).
//  Prefijo "us-" para evitar colisiones con otras pantallas.
// ============================================================================

export const usuariosCss = `
/* ────────────────────────────────────────────────────────────────────────── */
/*  HEADER                                                                    */
/* ────────────────────────────────────────────────────────────────────────── */

.us-header{
  display:flex;
  align-items:flex-start;
  justify-content:space-between;
  gap:1rem;
  margin-bottom:1.5rem;
}

.us-header-title{
  font-size:22px;
  font-weight:600;
  color:#1e1050;
  margin:0;
}

.us-header-sub{
  font-size:13px;
  color:#7b6aaa;
  margin-top:4px;
}

.us-new-btn{
  display:inline-flex;
  align-items:center;
  gap:6px;
  background:#4B2DAB;
  border:none;
  border-radius:8px;
  padding:9px 16px;
  font-size:13px;
  font-weight:500;
  color:#fff;
  cursor:pointer;
  transition:background .15s;
  font-family:inherit;
}
.us-new-btn:hover{background:#3a2090;}

/* ────────────────────────────────────────────────────────────────────────── */
/*  STATS                                                                     */
/* ────────────────────────────────────────────────────────────────────────── */

.us-stats{
  display:grid;
  grid-template-columns:repeat(4, 1fr);
  gap:12px;
  margin-bottom:1.5rem;
}

@media (max-width:900px){
  .us-stats{grid-template-columns:repeat(2, 1fr);}
}

.us-stat-card{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.15);
  border-radius:10px;
  padding:1rem 1.25rem;
}

.us-stat-label{
  font-size:12px;
  color:#7b6aaa;
  margin-bottom:4px;
}

.us-stat-value{
  font-size:28px;
  font-weight:600;
  color:#1e1050;
  line-height:1.1;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  FILTROS                                                                   */
/* ────────────────────────────────────────────────────────────────────────── */

.us-filters{
  display:flex;
  align-items:center;
  gap:10px;
  margin-bottom:1rem;
  flex-wrap:wrap;
}

.us-search{
  display:flex;
  align-items:center;
  gap:8px;
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:8px;
  padding:8px 12px;
  min-width:260px;
  flex:1;
  max-width:340px;
}

.us-search svg{flex-shrink:0;color:#9080bb;}

.us-search input{
  border:none;
  outline:none;
  background:transparent;
  font-family:inherit;
  font-size:13px;
  color:#1e1050;
  width:100%;
}

.us-search input::placeholder{color:#b5a8d3;}

.us-pills{
  display:flex;
  gap:6px;
  flex-wrap:wrap;
}

.us-pill{
  background:transparent;
  border:0.5px solid rgba(120,80,200,0.25);
  color:#5a4a8a;
  border-radius:999px;
  padding:7px 16px;
  font-size:12px;
  font-weight:500;
  cursor:pointer;
  transition:all .15s;
  font-family:inherit;
}

.us-pill:hover{background:#faf8ff;}

.us-pill.active{
  background:#4B2DAB;
  color:#fff;
  border-color:#4B2DAB;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  TABLA                                                                     */
/* ────────────────────────────────────────────────────────────────────────── */

.us-table-card{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.15);
  border-radius:10px;
  overflow:hidden;
}

.us-table{
  width:100%;
  border-collapse:collapse;
}

.us-table thead th{
  text-align:left;
  font-size:12px;
  font-weight:500;
  color:#7b6aaa;
  padding:12px 16px;
  background:#faf8ff;
  border-bottom:0.5px solid rgba(120,80,200,0.12);
}

.us-table tbody td{
  padding:14px 16px;
  font-size:13px;
  color:#1e1050;
  border-bottom:0.5px solid rgba(120,80,200,0.08);
  vertical-align:middle;
}

.us-table tbody tr:last-child td{border-bottom:none;}

.us-table tbody tr:hover{background:#faf8ff;}

.us-row-name{font-weight:500;}

.us-row-email{
  font-size:12px;
  color:#7b6aaa;
  margin-top:2px;
}

/* Avatar pequeño */
.us-mini-avatar{
  display:inline-flex;
  align-items:center;
  justify-content:center;
  width:32px;
  height:32px;
  border-radius:50%;
  background:#4B2DAB;
  color:#fff;
  font-size:13px;
  font-weight:500;
  margin-right:10px;
  vertical-align:middle;
}

.us-name-cell{
  display:flex;
  align-items:center;
}

/* Badge de rol */
.us-rol-badge{
  display:inline-block;
  padding:3px 10px;
  border-radius:999px;
  font-size:11px;
  font-weight:600;
  letter-spacing:0.02em;
}

.us-rol-admin{background:#EDE8FC;color:#3C3489;}
.us-rol-auditor{background:#E8F0FE;color:#1A4480;}
.us-rol-operador{background:#FFF4E5;color:#BA7517;}

/* Estado activo/inactivo */
.us-estado{
  display:inline-flex;
  align-items:center;
  gap:6px;
  font-size:12px;
}

.us-estado::before{
  content:'';
  width:7px;
  height:7px;
  border-radius:50%;
}

.us-estado.activo{color:#27500A;}
.us-estado.activo::before{background:#27500A;}

.us-estado.inactivo{color:#A32D2D;}
.us-estado.inactivo::before{background:#A32D2D;}

/* Acciones */
.us-actions{
  display:flex;
  gap:6px;
  justify-content:flex-end;
}

.us-action-btn{
  background:#fff;
  border:0.5px solid rgba(120,80,200,0.25);
  border-radius:6px;
  padding:5px 10px;
  font-size:11px;
  color:#4B2DAB;
  cursor:pointer;
  font-family:inherit;
  transition:all .15s;
}

.us-action-btn:hover:not(:disabled){
  background:#faf8ff;
  border-color:#7B52E8;
}

.us-action-btn:disabled{
  opacity:0.4;
  cursor:not-allowed;
}

.us-action-btn.danger{color:#A32D2D;}
.us-action-btn.danger:hover:not(:disabled){
  background:#FCEBEB;
  border-color:#A32D2D;
}

/* Empty / loading */
.us-empty{
  padding:3rem 1.5rem;
  text-align:center;
  color:#7b6aaa;
  font-size:13px;
}

/* ────────────────────────────────────────────────────────────────────────── */
/*  MODAL                                                                     */
/* ────────────────────────────────────────────────────────────────────────── */

.us-modal-overlay{
  position:fixed;
  inset:0;
  background:rgba(30,16,80,0.4);
  display:flex;
  align-items:center;
  justify-content:center;
  z-index:1000;
  padding:1rem;
  animation:us-fadeIn .15s ease;
}

@keyframes us-fadeIn{
  from{opacity:0;}
  to{opacity:1;}
}

.us-modal{
  background:#fff;
  border-radius:12px;
  max-width:520px;
  width:100%;
  max-height:90vh;
  overflow-y:auto;
  box-shadow:0 24px 60px rgba(30,16,80,0.25);
  animation:us-slideUp .2s ease;
}

@keyframes us-slideUp{
  from{transform:translateY(20px);opacity:0;}
  to{transform:translateY(0);opacity:1;}
}

.us-modal-header{
  display:flex;
  align-items:flex-start;
  justify-content:space-between;
  gap:1rem;
  padding:20px 24px 12px;
  border-bottom:0.5px solid rgba(120,80,200,0.1);
}

.us-modal-title{
  font-size:16px;
  font-weight:600;
  color:#1e1050;
  margin:0;
}

.us-modal-close{
  background:none;
  border:none;
  cursor:pointer;
  color:#9080bb;
  padding:0;
  width:24px;
  height:24px;
  display:flex;
  align-items:center;
  justify-content:center;
  flex-shrink:0;
  font-size:22px;
  line-height:1;
}
.us-modal-close:hover{color:#1e1050;}

.us-modal-body{
  padding:18px 24px;
}

.us-modal-footer{
  display:flex;
  justify-content:flex-end;
  gap:8px;
  padding:16px 24px 20px;
  border-top:0.5px solid rgba(120,80,200,0.08);
}

.us-form-field{margin-bottom:1rem;}

.us-form-field label{
  display:block;
  font-size:12px;
  color:#5a4a8a;
  margin-bottom:5px;
  font-weight:500;
}

.us-form-field input,
.us-form-field select{
  width:100%;
  padding:9px 12px;
  border-radius:8px;
  border:0.5px solid rgba(120,80,200,0.25);
  background:#faf8ff;
  font-size:13px;
  color:#1e1050;
  outline:none;
  font-family:inherit;
  transition:border-color .15s;
}

.us-form-field input:focus,
.us-form-field select:focus{
  border-color:#7B52E8;
  background:#fff;
}

.us-form-field input:disabled{
  background:#f4f4f4;
  color:#7b6aaa;
}

.us-form-field-hint{
  font-size:11px;
  color:#7b6aaa;
  margin-top:4px;
}

.us-form-checkbox{
  display:flex;
  align-items:center;
  gap:8px;
  cursor:pointer;
  font-size:13px;
  color:#1e1050;
}

.us-form-checkbox input{
  width:16px;
  height:16px;
  cursor:pointer;
}

.us-btn-sec{
  padding:9px 18px;
  border-radius:8px;
  border:0.5px solid rgba(120,80,200,0.3);
  background:#fff;
  font-size:13px;
  color:#4B2DAB;
  cursor:pointer;
  font-family:inherit;
}
.us-btn-sec:hover{background:#faf8ff;}

.us-btn-pri{
  padding:9px 18px;
  border-radius:8px;
  border:none;
  background:#4B2DAB;
  font-size:13px;
  color:#fff;
  cursor:pointer;
  font-weight:500;
  font-family:inherit;
  display:inline-flex;
  align-items:center;
  gap:8px;
}
.us-btn-pri:hover:not(:disabled){background:#3a2090;}
.us-btn-pri:disabled{opacity:0.5;cursor:not-allowed;}

.us-spinner{
  width:14px;
  height:14px;
  border:2px solid rgba(255,255,255,0.3);
  border-top-color:#fff;
  border-radius:50%;
  animation:us-spin .7s linear infinite;
}

@keyframes us-spin{to{transform:rotate(360deg);}}

/* Mensajes de feedback */
.us-feedback{
  padding:10px 12px;
  border-radius:8px;
  font-size:13px;
  margin-bottom:1rem;
  animation:us-fadeIn .15s ease;
}

.us-feedback.error{
  background:#FCEBEB;
  color:#A32D2D;
  border:1px solid rgba(163,45,45,0.2);
}

.us-feedback.success{
  background:#EAF3DE;
  color:#27500A;
  border:1px solid rgba(39,80,10,0.15);
}

/* Password input con toggle ojo */
.us-pass-wrap{
  position:relative;
}
.us-pass-wrap input{padding-right:40px;}
.us-pass-toggle{
  position:absolute;
  right:10px;
  top:50%;
  transform:translateY(-50%);
  background:none;
  border:none;
  cursor:pointer;
  color:#9080bb;
  padding:2px;
  display:flex;
  align-items:center;
}
.us-pass-toggle:hover{color:#4B2DAB;}
`;