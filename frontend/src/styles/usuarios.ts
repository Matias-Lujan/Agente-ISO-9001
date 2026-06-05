// src/styles/usuarios.ts
// ============================================================================
//  Estilos de la pantalla Usuarios (ABM).
//  Usa las variables de tema de sharedCss (claro/oscuro). Prefijo "us-".
// ============================================================================

export const usuariosCss = `
/*  HEADER  */
.us-header{display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;margin-bottom:1.5rem;}
.us-header-title{font-size:22px;font-weight:600;color:var(--text);margin:0;}
.us-header-sub{font-size:13px;color:var(--text-muted);margin-top:4px;}
/* El botón "Nuevo usuario" usa .btn-pri (shared) para que sea idéntico a los
   de Nueva auditoría / Exportar informe en tamaño, posición y color. */

/*  STATS  */
.us-stats{display:grid;grid-template-columns:repeat(4, 1fr);gap:12px;margin-bottom:1.5rem;}
@media (max-width:900px){.us-stats{grid-template-columns:repeat(2, 1fr);}}
.us-stat-card{
  background:var(--surface);border:0.5px solid var(--border);border-radius:10px;padding:1rem 1.25rem;
  transition:background-color .2s,border-color .2s;
}
.us-stat-label{font-size:12px;color:var(--text-muted);margin-bottom:4px;}
.us-stat-value{font-size:28px;font-weight:600;color:var(--text);line-height:1.1;}

/*  FILTROS  */
.us-filters{display:flex;align-items:center;gap:10px;margin-bottom:1rem;flex-wrap:wrap;}
.us-search{
  display:flex;align-items:center;gap:8px;
  background:var(--surface);border:0.5px solid var(--border);border-radius:8px;
  padding:8px 12px;min-width:260px;flex:1;max-width:340px;
}
.us-search svg{flex-shrink:0;color:var(--text-muted);}
.us-search input{border:none;outline:none;background:transparent;font-family:inherit;font-size:13px;color:var(--text);width:100%;}
.us-search input::placeholder{color:var(--text-muted);}
.us-pills{display:flex;gap:6px;flex-wrap:wrap;}
.us-pill{
  background:transparent;border:0.5px solid var(--border);color:var(--text-label);
  border-radius:999px;padding:7px 16px;font-size:12px;font-weight:500;
  cursor:pointer;transition:all .15s;font-family:inherit;
}
.us-pill:hover{background:var(--surface-2);}
.us-pill.active{background:var(--primary);color:var(--on-primary);border-color:var(--primary);}

/*  TABLA  */
.us-table-card{
  background:var(--surface);border:0.5px solid var(--border);border-radius:10px;overflow:hidden;
  transition:background-color .2s,border-color .2s;
}
.us-table{width:100%;border-collapse:collapse;}
.us-table thead th{
  text-align:left;font-size:12px;font-weight:500;color:var(--text-muted);
  padding:12px 16px;background:var(--surface-2);border-bottom:0.5px solid var(--border);
}
.us-table tbody td{
  padding:14px 16px;font-size:13px;color:var(--text);
  border-bottom:0.5px solid var(--border-soft);vertical-align:middle;
}
.us-table tbody tr:last-child td{border-bottom:none;}
.us-table tbody tr:hover{background:var(--surface-hover);}
.us-row-name{font-weight:500;}
.us-row-email{font-size:12px;color:var(--text-muted);margin-top:2px;}

/* Avatar pequeño */
.us-mini-avatar{
  display:inline-flex;align-items:center;justify-content:center;
  width:32px;height:32px;border-radius:50%;background:var(--primary);color:#fff;
  font-size:13px;font-weight:500;margin-right:10px;vertical-align:middle;
}
.us-name-cell{display:flex;align-items:center;}

/* Badge de rol */
.us-rol-badge{display:inline-block;padding:3px 10px;border-radius:999px;font-size:11px;font-weight:600;letter-spacing:0.02em;}
.us-rol-admin{background:var(--info-bg);color:var(--info-fg);}
.us-rol-auditor{background:var(--blue-bg);color:var(--blue-fg);}
.us-rol-operador{background:var(--warn-bg);color:var(--warn-fg);}

/* Estado activo/inactivo */
.us-estado{display:inline-flex;align-items:center;gap:6px;font-size:12px;}
.us-estado::before{content:'';width:7px;height:7px;border-radius:50%;}
.us-estado.activo{color:var(--ok-fg);}
.us-estado.activo::before{background:var(--ok-fg);}
.us-estado.inactivo{color:var(--err-fg);}
.us-estado.inactivo::before{background:var(--err-fg);}

/* Acciones */
.us-actions{display:flex;gap:6px;justify-content:flex-end;}
.us-action-btn{
  background:var(--surface);border:0.5px solid var(--border);border-radius:6px;
  padding:5px 10px;font-size:11px;color:var(--primary);cursor:pointer;
  font-family:inherit;transition:all .15s;
}
.us-action-btn:hover:not(:disabled){background:var(--surface-2);border-color:var(--accent);}
.us-action-btn:disabled{opacity:0.4;cursor:not-allowed;}
.us-action-btn.danger{color:var(--err-fg);}
.us-action-btn.danger:hover:not(:disabled){background:var(--err-bg);border-color:var(--err-fg);}

/* Empty / loading */
.us-empty{padding:3rem 1.5rem;text-align:center;color:var(--text-muted);font-size:13px;}

/*  MODAL  */
.us-modal-overlay{
  position:fixed;inset:0;background:rgba(15,8,40,0.55);
  display:flex;align-items:center;justify-content:center;z-index:1000;
  padding:1rem;animation:us-fadeIn .15s ease;
}
@keyframes us-fadeIn{from{opacity:0;}to{opacity:1;}}
.us-modal{
  background:var(--surface);border-radius:12px;max-width:520px;width:100%;
  max-height:90vh;overflow-y:auto;box-shadow:0 24px 60px rgba(0,0,0,0.35);
  animation:us-slideUp .2s ease;
}
@keyframes us-slideUp{from{transform:translateY(20px);opacity:0;}to{transform:translateY(0);opacity:1;}}
.us-modal-header{
  display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;
  padding:20px 24px 12px;border-bottom:0.5px solid var(--border);
}
.us-modal-title{font-size:16px;font-weight:600;color:var(--text);margin:0;}
.us-modal-close{
  background:none;border:none;cursor:pointer;color:var(--text-muted);padding:0;
  width:24px;height:24px;display:flex;align-items:center;justify-content:center;
  flex-shrink:0;font-size:22px;line-height:1;
}
.us-modal-close:hover{color:var(--text);}
.us-modal-body{padding:18px 24px;}
.us-modal-footer{
  display:flex;justify-content:flex-end;gap:8px;padding:16px 24px 20px;
  border-top:0.5px solid var(--border-soft);
}

.us-form-field{margin-bottom:1rem;}
.us-form-field label{display:block;font-size:12px;color:var(--text-label);margin-bottom:5px;font-weight:500;}
.us-form-field input,.us-form-field select{
  width:100%;padding:9px 12px;border-radius:8px;border:0.5px solid var(--border);
  background:var(--surface-2);font-size:13px;color:var(--text);outline:none;
  font-family:inherit;transition:border-color .15s,background-color .2s;
}
.us-form-field input:focus,.us-form-field select:focus{border-color:var(--accent);background:var(--surface);}
.us-form-field input:disabled{background:var(--surface-2);color:var(--text-muted);opacity:0.8;}
.us-form-field-hint{font-size:11px;color:var(--text-muted);margin-top:4px;}
.us-form-checkbox{display:flex;align-items:center;gap:8px;cursor:pointer;font-size:13px;color:var(--text);}
.us-form-checkbox input{width:16px;height:16px;cursor:pointer;}

.us-btn-sec{
  padding:9px 18px;border-radius:8px;border:0.5px solid var(--border);
  background:var(--surface);font-size:13px;color:var(--primary);cursor:pointer;font-family:inherit;
}
.us-btn-sec:hover{background:var(--surface-2);}
.us-btn-pri{
  padding:9px 18px;border-radius:8px;border:none;background:var(--primary);
  font-size:13px;color:var(--on-primary);cursor:pointer;font-weight:500;font-family:inherit;
  display:inline-flex;align-items:center;gap:8px;
}
.us-btn-pri:hover:not(:disabled){background:var(--primary-hover);}
.us-btn-pri:disabled{opacity:0.5;cursor:not-allowed;}

.us-spinner{
  width:14px;height:14px;border:2px solid rgba(255,255,255,0.3);
  border-top-color:#fff;border-radius:50%;animation:us-spin .7s linear infinite;
}
@keyframes us-spin{to{transform:rotate(360deg);}}

/* Mensajes de feedback */
.us-feedback{padding:10px 12px;border-radius:8px;font-size:13px;margin-bottom:1rem;animation:us-fadeIn .15s ease;}
.us-feedback.error{background:var(--err-bg);color:var(--err-fg);border:1px solid var(--border-soft);}
.us-feedback.success{background:var(--ok-bg);color:var(--ok-fg);border:1px solid var(--border-soft);}

/* Password input con toggle ojo */
.us-pass-wrap{position:relative;}
.us-pass-wrap input{padding-right:40px;}
.us-pass-toggle{
  position:absolute;right:10px;top:50%;transform:translateY(-50%);
  background:none;border:none;cursor:pointer;color:var(--text-muted);padding:2px;
  display:flex;align-items:center;
}
.us-pass-toggle:hover{color:var(--primary);}
`;
