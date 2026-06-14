// CSS de la pantalla Informes. Usa las variables de tema de styles/shared.ts,
// así que respeta el modo claro/oscuro automáticamente.
export const informesCss = `
/* ----- Filtros ----- */
.in-filters{display:flex;gap:8px;flex-wrap:wrap;margin-bottom:1.3rem;}
.in-pill{
  padding:7px 15px;border-radius:999px;font-size:12.5px;font-weight:500;cursor:pointer;
  border:0.5px solid var(--border);background:var(--surface);color:var(--text-muted);transition:all .15s;
}
.in-pill:hover{color:var(--text);}
.in-pill.active{background:var(--primary);color:var(--on-primary);border-color:var(--primary);}

/* ----- Tabla ----- */
.in-table-wrap{
  background:var(--surface);border:0.5px solid var(--border);border-radius:12px;overflow:hidden;
}
.in-table{width:100%;border-collapse:collapse;}
.in-table thead th{
  text-align:left;font-size:11px;font-weight:600;color:var(--text-muted);
  text-transform:uppercase;letter-spacing:.04em;padding:13px 18px;background:var(--surface-3);
}
.in-table tbody td{
  padding:14px 18px;font-size:13px;color:var(--text);
  border-top:0.5px solid var(--border-soft);vertical-align:middle;
}
.in-table tbody tr{transition:background-color .12s;}
.in-table tbody tr:hover{background:var(--surface-hover);}
.in-pj{font-weight:600;}
.in-badge{display:inline-block;padding:3px 10px;border-radius:999px;font-size:11px;font-weight:600;white-space:nowrap;}
.in-badge.gen{background:var(--ok-bg);color:var(--ok-fg);}
.in-badge.tipo{background:var(--info-bg);color:var(--info-fg);}

.in-acts{display:flex;gap:4px;justify-content:flex-end;}
.in-link{
  display:inline-flex;align-items:center;gap:6px;font-size:12.5px;font-weight:600;cursor:pointer;
  color:var(--primary);background:none;border:none;font-family:inherit;padding:6px 10px;border-radius:8px;transition:background-color .12s;
}
.in-link:hover{background:var(--info-bg);}
.in-link svg{width:14px;height:14px;}

/* ----- Empty / loading ----- */
.in-empty{
  padding:3rem 1.5rem;text-align:center;color:var(--text-muted);font-size:13px;
  background:var(--surface);border:0.5px solid var(--border);border-radius:12px;
}

/* ----- Modal vista previa ----- */
.in-overlay{
  position:fixed;inset:0;background:rgba(15,8,40,0.55);
  display:flex;align-items:center;justify-content:center;z-index:1000;padding:1rem;animation:in-fade .15s ease;
}
@keyframes in-fade{from{opacity:0;}to{opacity:1;}}
.in-modal{
  background:var(--surface);border-radius:12px;max-width:760px;width:100%;
  max-height:90vh;display:flex;flex-direction:column;box-shadow:0 24px 60px rgba(0,0,0,0.35);animation:in-up .2s ease;
}
@keyframes in-up{from{transform:translateY(20px);opacity:0;}to{transform:translateY(0);opacity:1;}}
.in-modal-head{
  display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;
  padding:20px 24px 14px;border-bottom:0.5px solid var(--border);
}
.in-modal-title{font-size:16px;font-weight:600;color:var(--text);}
.in-modal-sub{font-size:12px;color:var(--text-muted);margin-top:3px;}
.in-modal-close{
  background:none;border:none;cursor:pointer;color:var(--text-muted);
  width:24px;height:24px;font-size:22px;line-height:1;flex-shrink:0;
}
.in-modal-close:hover{color:var(--text);}
.in-modal-body{padding:18px 24px;overflow-y:auto;}
.in-contenido{
  font-size:13px;line-height:1.6;color:var(--text);white-space:pre-wrap;word-break:break-word;
  font-family:ui-monospace,SFMono-Regular,Menlo,monospace;
}
.in-modal-foot{display:flex;justify-content:flex-end;gap:8px;padding:14px 24px 20px;border-top:0.5px solid var(--border-soft);}

.in-proj-chip{display:inline-flex;align-items:center;gap:8px;background:var(--info-bg);color:var(--info-fg);border-radius:999px;padding:7px 9px 7px 14px;font-size:12.5px;font-weight:500;margin-bottom:1rem;}
.in-proj-chip b{font-weight:700;}
.in-proj-chip button{background:rgba(0,0,0,.1);border:none;color:inherit;width:19px;height:19px;border-radius:50%;cursor:pointer;font-size:13px;line-height:1;display:flex;align-items:center;justify-content:center;}
`;