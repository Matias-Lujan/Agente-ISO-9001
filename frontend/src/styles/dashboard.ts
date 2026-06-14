// Estilos del dashboard del auditor.
//
// Usa las variables de tema definidas en sharedCss (claro/oscuro). No hay
// colores fijos: todo sale de var(--token), así el dashboard cambia de tema
// junto con el resto de la app.
export const dashboardCss = `
/* ----- Métricas ----- */
.dash-metrics{
  display:grid;grid-template-columns:repeat(4,minmax(0,1fr));
  gap:12px;margin-bottom:1.5rem;
}
.dash-metric{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;padding:1rem;
  transition:background-color .2s,border-color .2s;
}
.dash-metric-label{font-size:12px;margin-bottom:6px;color:var(--text-muted);}
.dash-metric-value{font-size:28px;font-weight:600;color:var(--text);line-height:1.1;}
.dash-metric-value.dash-v-nc{color:var(--err-fg);}
.dash-metric-value.dash-v-warn{color:var(--warn-fg);}
.dash-metric-value.dash-v-ok{color:var(--ok-fg);}
.dash-metric-sub{font-size:11px;margin-top:4px;color:var(--text-muted);}

/* ----- Secciones ----- */
.dash-section-title{
  font-size:11px;font-weight:500;margin-bottom:0.75rem;
  text-transform:uppercase;letter-spacing:.04em;color:var(--text-muted);
}

/* ----- Proyectos recientes ----- */
.dash-projects{display:flex;flex-direction:column;gap:8px;margin-bottom:1.5rem;}
.dash-project-row{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;padding:0.875rem 1rem;
  display:flex;align-items:center;gap:12px;transition:all .15s;
}
.dash-project-row:hover{border-color:var(--accent);}
.dash-proj-name{font-size:14px;font-weight:500;color:var(--text);}
.dash-proj-date{font-size:12px;margin-top:2px;color:var(--text-muted);}
.dash-proj-count{font-size:12px;color:var(--text-muted);white-space:nowrap;}

/* ----- Badges ----- */
.dash-badge{
  display:inline-flex;align-items:center;
  font-size:11px;font-weight:500;padding:3px 8px;
  border-radius:20px;white-space:nowrap;
}
.dash-badge-nc{background:var(--err-bg);color:var(--err-fg);}
.dash-badge-ok{background:var(--ok-bg);color:var(--ok-fg);}
.dash-badge-pen{background:var(--warn-bg);color:var(--warn-fg);}
.dash-badge-om{background:var(--info-bg);color:var(--info-fg);}

/* ----- Tabla de últimos hallazgos ----- */
.dash-hallazgos{
  background:var(--surface);border:0.5px solid var(--border);
  border-radius:10px;overflow:hidden;
  transition:background-color .2s,border-color .2s;
}
.dash-hall-header{
  display:grid;grid-template-columns:2fr 1fr 1fr 1fr;
  padding:0.625rem 1rem;font-size:11px;font-weight:500;
  text-transform:uppercase;letter-spacing:.04em;
  background:var(--surface-3);color:var(--text-muted);
}
.dash-hall-row{
  display:grid;grid-template-columns:2fr 1fr 1fr 1fr;
  padding:0.75rem 1rem;align-items:center;font-size:13px;
  border-top:0.5px solid var(--border-soft);color:var(--text);transition:all .15s;
}
.dash-hall-row:hover{background:var(--surface-hover);}
.dash-hall-proj{font-size:13px;color:var(--text-muted);}
.dash-hall-date{font-size:13px;color:var(--text-muted);}

/* ----- Vacío ----- */
.dash-empty{padding:1.25rem;text-align:center;font-size:13px;color:var(--text-muted);}

/* ----- Modal de advertencia (rol sin permiso) ----- */
.dash-modal-overlay{
  position:fixed;inset:0;background:rgba(15,8,40,0.55);
  display:flex;align-items:center;justify-content:center;
  z-index:1000;padding:1rem;animation:dash-fade .15s ease;
}
@keyframes dash-fade{from{opacity:0;}to{opacity:1;}}
.dash-modal{
  background:var(--surface);border:0.5px solid var(--border);border-radius:12px;
  max-width:380px;width:100%;padding:1.75rem;text-align:center;
  box-shadow:0 24px 60px rgba(0,0,0,0.35);
}
.dash-modal-icon{font-size:32px;margin-bottom:0.5rem;line-height:1;}
.dash-modal-title{font-size:16px;font-weight:600;color:var(--text);margin:0 0 0.5rem;}
.dash-modal-text{font-size:13px;color:var(--text-muted);line-height:1.5;margin-bottom:1.25rem;}

/* ----- Responsive ----- */
@media (max-width:900px){
  .dash-metrics{grid-template-columns:repeat(2,minmax(0,1fr));}
}
@media (max-width:560px){
  .dash-metrics{grid-template-columns:1fr;}
  .dash-hall-header,.dash-hall-row{grid-template-columns:2fr 1fr;}
  .dash-hall-header > div:nth-child(3),
  .dash-hall-header > div:nth-child(4),
  .dash-hall-row > div:nth-child(3),
  .dash-hall-row > div:nth-child(4){display:none;}
}

/* ----- Cumplimiento general (semáforo + torta) ----- */
.dash-cumpl{display:flex;align-items:center;gap:1.6rem;flex-wrap:wrap;background:var(--surface);border:0.5px solid var(--border);border-radius:14px;padding:1.3rem 1.5rem;margin-bottom:1.6rem;}
.dash-sem{display:flex;align-items:center;gap:13px;flex:1;min-width:260px;}
.dash-light-stack{display:flex;flex-direction:column;gap:6px;background:var(--surface-2);border-radius:12px;padding:9px;}
.dash-lt{width:15px;height:15px;border-radius:50%;opacity:.16;}
.dash-lt.r{background:var(--err-fg);} .dash-lt.y{background:var(--warn-fg);} .dash-lt.g{background:var(--ok-fg);}
.dash-sem.rojo .dash-lt.r{opacity:1;box-shadow:0 0 10px var(--err-fg);}
.dash-sem.amarillo .dash-lt.y{opacity:1;box-shadow:0 0 10px var(--warn-fg);}
.dash-sem.verde .dash-lt.g{opacity:1;box-shadow:0 0 10px var(--ok-fg);}
.dash-sem-estado{font-size:17px;font-weight:700;}
.dash-sem.rojo .dash-sem-estado{color:var(--err-fg);}
.dash-sem.amarillo .dash-sem-estado{color:var(--warn-fg);}
.dash-sem.verde .dash-sem-estado{color:var(--ok-fg);}
.dash-sem-resumen{font-size:13px;color:var(--text-muted);margin-top:3px;max-width:46ch;line-height:1.45;}
.dash-donut{position:relative;width:112px;height:112px;flex-shrink:0;}
.dash-donut svg{transform:rotate(-90deg);}
.dash-donut .track{stroke:var(--surface-3);}
.dash-donut-center{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;}
.dash-donut-center .pct{font-size:21px;font-weight:700;color:var(--text);line-height:1;}
.dash-donut-center .cap{font-size:10px;color:var(--text-muted);margin-top:2px;}
.dash-dleg{display:flex;flex-direction:column;gap:7px;}
.dash-dleg .l{display:flex;align-items:center;gap:8px;font-size:12px;color:var(--text);}
.dash-dleg .sw{width:10px;height:10px;border-radius:3px;}
.dash-dleg b{margin-left:3px;}

/* ----- Controles (rango de fechas) ----- */
.dash-controls{display:flex;gap:8px;flex-wrap:wrap;align-items:center;}
.dash-seg{display:flex;background:var(--surface-2);border:.5px solid var(--border);border-radius:9px;padding:3px;gap:2px;}
.dash-seg button{border:none;background:none;font-family:inherit;font-size:12px;font-weight:600;color:var(--text-muted);padding:6px 11px;border-radius:7px;cursor:pointer;}
.dash-seg button.on{background:var(--surface);color:var(--text);box-shadow:0 1px 3px rgba(0,0,0,.08);}

/* ----- Grilla y tarjetas de gráficos ----- */
.dash-grid2{display:grid;grid-template-columns:1fr 1fr;gap:1.2rem;margin-bottom:1.2rem;}
@media(max-width:980px){.dash-grid2{grid-template-columns:1fr;}}
.dash-card{background:var(--surface);border:.5px solid var(--border);border-radius:14px;padding:1.3rem 1.5rem;margin-bottom:1.2rem;}
.dash-card h2{font-family:"Syne",sans-serif;font-size:15.5px;font-weight:700;margin-bottom:1.1rem;display:flex;align-items:center;justify-content:space-between;gap:8px;}
.dash-card h2 .hint{font-size:11px;font-weight:500;color:var(--text-muted);}

/* Donut con leyenda */
.dash-dwrap{display:flex;align-items:center;gap:1.4rem;flex-wrap:wrap;}
.dash-dchart{position:relative;width:150px;height:150px;flex-shrink:0;}
.dash-dchart svg{transform:rotate(-90deg);}
.dash-dchart .track{stroke:var(--surface-3);}
.dash-dchart .center{position:absolute;inset:0;display:flex;flex-direction:column;align-items:center;justify-content:center;}
.dash-dchart .tot{font-family:"Syne",sans-serif;font-size:26px;font-weight:700;line-height:1;}
.dash-dchart .cap{font-size:10.5px;color:var(--text-muted);margin-top:3px;}
.dash-legend{display:flex;flex-direction:column;gap:9px;flex:1;min-width:150px;}
.dash-leg{display:flex;align-items:center;gap:9px;font-size:12.5px;}
.dash-leg .sw{width:10px;height:10px;border-radius:3px;flex-shrink:0;}
.dash-leg .lab{flex:1;color:var(--text);}.dash-leg .val{font-weight:700;}
.dash-leg .pct{font-size:11px;color:var(--text-muted);width:34px;text-align:right;}

/* Barras */
.dash-bars{display:flex;flex-direction:column;gap:.75rem;}
.dash-bar-row{display:grid;grid-template-columns:160px 1fr 44px;align-items:center;gap:12px;}
.dash-bar-row.ag{grid-template-columns:170px 1fr 30px;}
.dash-bar-name{font-size:12px;color:var(--text);white-space:nowrap;overflow:hidden;text-overflow:ellipsis;}
.dash-bar-track{height:11px;border-radius:999px;background:var(--surface-3);overflow:hidden;}
.dash-bar-fill{height:100%;border-radius:999px;transition:width .7s ease;}
.dash-bar-fill.ok{background:var(--ok-fg);}.dash-bar-fill.warn{background:var(--warn-fg);}.dash-bar-fill.err{background:var(--err-fg);}.dash-bar-fill.acc{background:var(--primary);}
.dash-bar-val{font-size:12px;font-weight:700;text-align:right;}
.dash-bar-note{font-size:11px;color:var(--text-muted);margin-top:1rem;display:flex;gap:14px;flex-wrap:wrap;}
.dash-bar-note span{display:inline-flex;align-items:center;gap:6px;}.dash-bar-note i{width:9px;height:9px;border-radius:3px;}

/* Evolución (área) */
.dash-area-x{display:flex;justify-content:space-between;margin-top:6px;font-size:11px;color:var(--text-muted);}

/* Atención */
.dash-att-row{display:grid;grid-template-columns:1fr auto auto 18px;align-items:center;gap:12px;padding:11px 4px;border-bottom:.5px solid var(--border-soft);cursor:pointer;}
.dash-att-row:last-child{border-bottom:none;}
.dash-att-row:hover{background:var(--surface-hover);}
.dash-att-name{font-size:13px;font-weight:600;}
.dash-att-name small{display:block;font-weight:400;color:var(--text-muted);font-size:11px;margin-top:1px;}
.dash-att-go{color:var(--text-muted);display:flex;}.dash-att-go svg{width:16px;height:16px;}
.dash-att-row:hover .dash-att-go{color:var(--primary);}

/* Skeleton */
.dash-sk{background:linear-gradient(90deg,var(--surface-3) 25%,var(--surface-2) 37%,var(--surface-3) 63%);background-size:400% 100%;animation:dash-sh 1.3s ease infinite;border-radius:8px;}
@keyframes dash-sh{0%{background-position:100% 0;}100%{background-position:-100% 0;}}
.dash-sk-line{height:12px;margin:8px 0;}.dash-sk-donut{width:150px;height:150px;border-radius:50%;}

/* Botón secundario (Exportar PDF) */
.btn-ghost{display:inline-flex;align-items:center;padding:9px 16px;border-radius:8px;border:0.5px solid var(--border);background:transparent;color:var(--text-muted);font-size:13px;font-weight:500;font-family:inherit;cursor:pointer;}
.btn-ghost:hover:not(:disabled){color:var(--text);}
.btn-ghost:disabled{opacity:.55;cursor:default;}
`;