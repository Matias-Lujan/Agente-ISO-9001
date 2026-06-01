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
`;
