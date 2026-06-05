// ============================================================================
//  Estilos de la pantalla Hallazgos.
//  Usa las variables de tema de sharedCss (claro/oscuro). Patron:
//  CSS-in-string + useInjectStyle.
// ============================================================================

export const hallazgosCss = `
/*  HEADER  */
.hz-header{
  display:flex;align-items:flex-start;justify-content:space-between;
  gap:1rem;margin-bottom:1.5rem;
}
.hz-header-title{font-size:22px;font-weight:600;color:var(--text);margin:0;}
.hz-header-sub{font-size:13px;color:var(--text-muted);margin-top:4px;}
/* El botón "Exportar informe" usa .btn-pri (shared) para que sea idéntico a los
   de Nueva auditoría / Nuevo usuario en tamaño, posición y color. */

/*  CARDS RESUMEN  */
.hz-stats{display:grid;grid-template-columns:repeat(4, 1fr);gap:12px;margin-bottom:1.5rem;}
@media (max-width:900px){.hz-stats{grid-template-columns:repeat(2, 1fr);}}
.hz-stat-card{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;padding:1rem 1.25rem;
  transition:background-color .2s,border-color .2s;
}
.hz-stat-label{font-size:12px;color:var(--text-muted);margin-bottom:4px;}
.hz-stat-value{font-size:28px;font-weight:600;color:var(--text);line-height:1.1;margin-bottom:4px;}
.hz-stat-value.nc{color:var(--err-fg);}
.hz-stat-value.obs{color:var(--warn-fg);}
.hz-stat-value.om{color:var(--ok-fg);}
.hz-stat-meta{font-size:11px;color:var(--text-muted);}

/*  FILTROS Y BUSCADOR  */
.hz-filters-bar{display:flex;align-items:center;gap:10px;margin-bottom:1rem;flex-wrap:wrap;}
.hz-search{
  display:flex;align-items:center;gap:8px;
  background:var(--surface);border:0.5px solid var(--border);border-radius:8px;
  padding:8px 12px;min-width:280px;flex:1;max-width:340px;
}
.hz-search svg{flex-shrink:0;color:var(--text-muted);}
.hz-search input{
  border:none;outline:none;background:transparent;font-family:inherit;
  font-size:13px;color:var(--text);width:100%;
}
.hz-search input::placeholder{color:var(--text-muted);}

.hz-pills{display:flex;gap:6px;flex-wrap:wrap;}
.hz-pill{
  background:transparent;border:0.5px solid var(--border);color:var(--text-label);
  border-radius:999px;padding:7px 16px;font-size:12px;font-weight:500;
  cursor:pointer;transition:all .15s;font-family:inherit;
}
.hz-pill:hover{background:var(--surface-2);}
.hz-pill.active{background:var(--primary);color:var(--on-primary);border-color:var(--primary);}

/*  TABLA  */
.hz-table-card{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;overflow:hidden;
  transition:background-color .2s,border-color .2s;
}
.hz-table{width:100%;border-collapse:collapse;}
.hz-table thead th{
  text-align:left;font-size:12px;font-weight:500;color:var(--text-muted);
  padding:12px 16px;background:var(--surface-2);
  border-bottom:0.5px solid var(--border);
}
.hz-table tbody td{
  padding:14px 16px;font-size:13px;color:var(--text);
  border-bottom:0.5px solid var(--border-soft);vertical-align:top;
}
.hz-table tbody tr:last-child td{border-bottom:none;}
.hz-table tbody tr:hover{background:var(--surface-hover);}
.hz-row-title{font-weight:500;margin-bottom:3px;}
.hz-row-meta{font-size:11px;color:var(--text-muted);}

/* Badges */
.hz-badge{display:inline-block;padding:3px 10px;border-radius:999px;font-size:11px;font-weight:600;letter-spacing:0.03em;}
.hz-badge.nc{background:var(--err-bg);color:var(--err-fg);}
.hz-badge.obs{background:var(--warn-bg);color:var(--warn-fg);}
.hz-badge.om{background:var(--ok-bg);color:var(--ok-fg);}

.hz-estado-dot{display:inline-flex;align-items:center;gap:6px;font-size:12px;color:var(--text);}
.hz-estado-dot::before{content:'';width:7px;height:7px;border-radius:50%;background:var(--err-fg);}
.hz-estado-dot.estado-abierto::before    {background:var(--err-fg);}
.hz-estado-dot.estado-en-revision::before{background:var(--warn-fg);}
.hz-estado-dot.estado-resuelto::before   {background:var(--ok-fg);}

.hz-ver-btn{
  background:var(--surface);border:0.5px solid var(--border);border-radius:6px;
  padding:5px 12px;font-size:12px;color:var(--primary);cursor:pointer;
  font-family:inherit;transition:all .15s;
}
.hz-ver-btn:hover{background:var(--surface-2);border-color:var(--accent);}

/*  PAGINACION  */
.hz-pagination{
  display:flex;align-items:center;justify-content:space-between;
  padding:14px 16px;border-top:0.5px solid var(--border-soft);
}
.hz-pagination-info{font-size:12px;color:var(--text-muted);}
.hz-pagination-pages{display:flex;gap:4px;}
.hz-page-btn{
  width:30px;height:30px;display:flex;align-items:center;justify-content:center;
  background:transparent;border:0.5px solid transparent;border-radius:6px;
  font-size:12px;color:var(--text-label);cursor:pointer;font-family:inherit;
}
.hz-page-btn:hover{background:var(--surface-2);}
.hz-page-btn.active{background:var(--primary);color:var(--on-primary);font-weight:500;}

/*  EMPTY STATE  */
.hz-empty{padding:3rem 1.5rem;text-align:center;color:var(--text-muted);font-size:13px;}

/*  MODAL DE DETALLE  */
.hz-modal-overlay{
  position:fixed;inset:0;background:rgba(15,8,40,0.55);
  display:flex;align-items:center;justify-content:center;z-index:1000;
  padding:1rem;animation:hz-fadeIn .15s ease;
}
@keyframes hz-fadeIn{from{opacity:0;}to{opacity:1;}}
.hz-modal{
  background:var(--surface);border-radius:12px;max-width:540px;width:100%;
  max-height:90vh;overflow-y:auto;
  box-shadow:0 24px 60px rgba(0,0,0,0.35);animation:hz-slideUp .2s ease;
}
@keyframes hz-slideUp{from{transform:translateY(20px);opacity:0;}to{transform:translateY(0);opacity:1;}}
.hz-modal-header{
  display:flex;align-items:flex-start;justify-content:space-between;gap:1rem;
  padding:20px 24px 12px;border-bottom:0.5px solid var(--border);
}
.hz-modal-title{font-size:16px;font-weight:600;color:var(--text);margin:0;line-height:1.3;}
.hz-modal-close{
  background:none;border:none;cursor:pointer;color:var(--text-muted);padding:0;
  width:24px;height:24px;display:flex;align-items:center;justify-content:center;
  flex-shrink:0;font-size:22px;line-height:1;
}
.hz-modal-close:hover{color:var(--text);}
.hz-modal-body{padding:18px 24px 24px;}
.hz-modal-row{display:flex;flex-direction:column;gap:4px;margin-bottom:14px;}
.hz-modal-label{font-size:11px;font-weight:600;color:var(--text-muted);text-transform:uppercase;letter-spacing:0.05em;}
.hz-modal-value{font-size:13px;color:var(--text);line-height:1.5;}
.hz-modal-tags{display:flex;gap:8px;margin-bottom:16px;}
`;
