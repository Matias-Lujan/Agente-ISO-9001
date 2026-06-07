// CSS de la pantalla Proyectos. Usa las variables de tema definidas en
// styles/shared.ts, así que respeta el modo claro/oscuro automáticamente.
export const proyectosCss = `
/* ----- Grid de tarjetas ----- */
.pr-grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(320px,1fr));gap:1rem;}
.pr-card{
  background:var(--surface);border:0.5px solid var(--border);border-radius:12px;
  padding:1.1rem 1.2rem;transition:border-color .2s,transform .15s,box-shadow .15s;
}
.pr-card:hover{transform:translateY(-2px);box-shadow:0 12px 28px -14px rgba(30,16,80,0.3);}
.pr-top{display:flex;align-items:flex-start;justify-content:space-between;gap:10px;margin-bottom:5px;}
.pr-name{font-size:15px;font-weight:600;color:var(--text);line-height:1.3;}
.pr-meta{font-size:11px;color:var(--text-muted);margin-bottom:.9rem;}

/* ----- Badges ----- */
.pr-badge{display:inline-block;padding:3px 10px;border-radius:999px;font-size:11px;font-weight:600;white-space:nowrap;}
.pr-badge.tipo{background:var(--info-bg);color:var(--info-fg);}
.pr-badge.ok{background:var(--ok-bg);color:var(--ok-fg);}
.pr-badge.off{background:var(--err-bg);color:var(--err-fg);}
.pr-badge.gray{background:var(--surface-3);color:var(--text-muted);}

/* ----- Operadores asignados ----- */
.pr-assignee{display:flex;align-items:center;gap:9px;font-size:12px;margin-bottom:.7rem;}
.pr-mini{
  width:26px;height:26px;border-radius:50%;background:var(--sb-avatar-bg);color:#fff;
  display:flex;align-items:center;justify-content:center;font-size:10px;font-weight:700;flex-shrink:0;
}
.pr-stack{display:flex;}
.pr-stack .pr-mini{margin-left:-8px;border:2px solid var(--surface);}
.pr-stack .pr-mini:first-child{margin-left:0;}
.pr-who{color:var(--text);font-weight:600;}
.pr-role{color:var(--text-muted);}
.pr-assignee.none{color:var(--text-muted);}
.pr-ph{
  width:26px;height:26px;border-radius:50%;border:1.5px dashed var(--border);
  display:flex;align-items:center;justify-content:center;flex-shrink:0;
}
.pr-ph svg{width:14px;height:14px;opacity:.55;}

/* ----- Chips de integraciones ----- */
.pr-chips{display:flex;flex-wrap:wrap;gap:6px;margin-bottom:.7rem;}
.pr-chip{
  display:inline-flex;align-items:center;gap:5px;font-size:11px;font-weight:600;
  padding:4px 9px;border-radius:7px;background:var(--surface-2);color:var(--text-muted);
}
.pr-chip .dot{width:7px;height:7px;border-radius:50%;flex-shrink:0;}

/* ----- Línea de procedimiento ----- */
.pr-proc{padding:.6rem .75rem;border-radius:8px;background:var(--surface-3);}
.pr-proc .tag{display:block;font-size:11px;font-weight:700;color:var(--primary);margin-bottom:2px;}
.pr-proc .sub{font-size:11px;color:var(--text-muted);}

.pr-foot{
  display:flex;align-items:center;justify-content:space-between;
  margin-top:.9rem;padding-top:.8rem;border-top:0.5px solid var(--border-soft);
}
.pr-hours{font-size:11px;color:var(--text-muted);}
.pr-hours b{color:var(--text);}

/* ----- Filtros ----- */
.pr-filters{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:1.3rem;}
.pr-pill{
  padding:7px 15px;border-radius:999px;font-size:12.5px;font-weight:500;cursor:pointer;
  border:0.5px solid var(--border);background:var(--surface);color:var(--text-muted);transition:all .15s;
}
.pr-pill:hover{color:var(--text);}
.pr-pill.active{background:var(--primary);color:var(--on-primary);border-color:var(--primary);}

/* ----- Empty ----- */
.pr-empty{
  padding:3rem 1.5rem;text-align:center;color:var(--text-muted);font-size:13px;
  background:var(--surface);border:0.5px solid var(--border);border-radius:12px;
}

/* ----- Modal (mismo estilo que el ABM de usuarios) ----- */
.pr-overlay{
  position:fixed;inset:0;background:rgba(15,8,40,0.55);
  display:flex;align-items:center;justify-content:center;z-index:1000;padding:1rem;animation:pr-fade .15s ease;
}
@keyframes pr-fade{from{opacity:0;}to{opacity:1;}}
.pr-modal{
  background:var(--surface);border-radius:12px;max-width:560px;width:100%;
  max-height:90vh;display:flex;flex-direction:column;box-shadow:0 24px 60px rgba(0,0,0,0.35);animation:pr-up .2s ease;
}
@keyframes pr-up{from{transform:translateY(20px);opacity:0;}to{transform:translateY(0);opacity:1;}}
.pr-modal-head{
  display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;
  padding:20px 24px 12px;border-bottom:0.5px solid var(--border);
}
.pr-modal-title{font-size:16px;font-weight:600;color:var(--text);}
.pr-modal-sub{font-size:12px;color:var(--text-muted);margin-top:3px;}
.pr-modal-close{
  background:none;border:none;cursor:pointer;color:var(--text-muted);
  width:24px;height:24px;font-size:22px;line-height:1;flex-shrink:0;
}
.pr-modal-close:hover{color:var(--text);}
.pr-modal-body{padding:18px 24px;overflow-y:auto;}
.pr-modal-foot{display:flex;justify-content:flex-end;gap:8px;padding:16px 24px 20px;border-top:0.5px solid var(--border-soft);}

.pr-fld{margin-bottom:1rem;}
.pr-fld:last-child{margin-bottom:0;}
.pr-fld label{display:block;font-size:12px;color:var(--text-label);margin-bottom:5px;font-weight:500;}
.pr-fld input,.pr-fld textarea,.pr-fld select{
  width:100%;padding:9px 12px;border-radius:8px;border:0.5px solid var(--border);
  background:var(--surface-2);font-size:13px;color:var(--text);outline:none;font-family:inherit;
  transition:border-color .15s,background-color .2s;
}
.pr-fld textarea{resize:vertical;min-height:62px;}
.pr-fld input:focus,.pr-fld textarea:focus,.pr-fld select:focus{border-color:var(--accent);background:var(--surface);}
.pr-fld .hint{font-size:11px;color:var(--text-muted);margin-top:4px;}
.pr-row2{display:grid;grid-template-columns:1fr 1fr;gap:12px;}

.pr-opsel{display:flex;flex-wrap:wrap;gap:8px;}
.pr-opchip{
  display:inline-flex;align-items:center;gap:6px;padding:7px 12px;border-radius:999px;
  border:0.5px solid var(--border);background:var(--surface-2);color:var(--text-muted);
  font-family:inherit;font-size:12px;font-weight:600;cursor:pointer;transition:all .15s;
}
.pr-opchip:hover{border-color:var(--accent);}
.pr-opchip.sel{background:var(--primary);border-color:var(--primary);color:var(--on-primary);}

.btn-ghost{
  padding:9px 18px;border-radius:8px;border:0.5px solid var(--border);background:transparent;
  color:var(--text-muted);font-size:13px;font-weight:500;font-family:inherit;cursor:pointer;
}
.btn-ghost:hover{color:var(--text);}
`;