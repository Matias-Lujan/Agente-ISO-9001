// Estilos del dashboard del auditor.
//
// Migrado del prototipo (AuditoriaDashboardScreen.css) al patrón del proyecto:
// string inyectado una sola vez vía useInjectStyle, igual que sharedCss /
// hallazgosCss. Diferencias respecto al .css original del prototipo:
//
//   - Se eliminaron los resets GLOBALES (* {...}, html/body) y el :root global
//     porque pisaban el design system de sharedCss y afectaban a toda la app.
//     Las variables de color viven scopeadas dentro de .dash-root.
//   - Se eliminó el sidebar propio: el layout ya lo provee ShellLayout.
//   - Todas las clases llevan prefijo .dash-* para no colisionar con .sidebar,
//     .nav-item, .badge, etc. del resto del frontend.
//   - Se quitó la barra de "compliance %" (el backend no expone esa métrica).
export const dashboardCss = `
.dash-root {
  /* Tema claro (default) — variables scopeadas, no global */
  --dash-bg: #f0f2f5;
  --dash-surface: #ffffff;
  --dash-surface-2: #f8f9fb;
  --dash-text: #1a1d2e;
  --dash-text-2: #7f8c8d;
  --dash-border: #e0e3e8;
  --dash-primary: #5b7aff;
  --dash-primary-dark: #4a68dd;
  --dash-danger: #ff4757;
  --dash-warning: #ffa502;
  --dash-success: #2ed573;
  --dash-info: #54a0ff;
  --dash-shadow-sm: 0 2px 8px rgba(0, 0, 0, 0.08);
  --dash-shadow-md: 0 4px 16px rgba(0, 0, 0, 0.12);

  display: flex;
  flex-direction: column;
  min-height: 100%;
  background: var(--dash-bg);
  color: var(--dash-text);
}

.dash-root.dash-dark {
  --dash-bg: #1a1d2e;
  --dash-surface: #252d42;
  --dash-surface-2: #2d3a5c;
  --dash-text: #e4e8f0;
  --dash-text-2: #a0a8b8;
  --dash-border: #3a4456;
  --dash-shadow-sm: 0 2px 8px rgba(0, 0, 0, 0.2);
  --dash-shadow-md: 0 4px 16px rgba(0, 0, 0, 0.3);
}

/* ----- Header ----- */
.dash-header {
  background: var(--dash-surface);
  border-bottom: 1px solid var(--dash-border);
  padding: 2rem;
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 2rem;
}
.dash-header-left { flex: 1; }
.dash-title {
  font-size: 2rem;
  font-weight: 700;
  margin-bottom: 0.5rem;
  color: var(--dash-text);
}
.dash-subtitle {
  font-size: 0.95rem;
  color: var(--dash-text-2);
}
.dash-header-right {
  display: flex;
  gap: 1rem;
  align-items: center;
}
.dash-btn-theme {
  background: transparent;
  border: 1px solid var(--dash-border);
  color: var(--dash-text-2);
  padding: 0.6rem 1rem;
  border-radius: 5px;
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 500;
  transition: all 0.3s ease;
}
.dash-btn-theme:hover {
  border-color: var(--dash-primary);
  color: var(--dash-primary);
}
.dash-btn-new {
  background: var(--dash-primary);
  border: none;
  color: #fff;
  padding: 0.6rem 1.2rem;
  border-radius: 5px;
  cursor: pointer;
  font-size: 0.85rem;
  font-weight: 600;
  transition: all 0.3s ease;
}
.dash-btn-new:hover {
  background: var(--dash-primary-dark);
  box-shadow: var(--dash-shadow-md);
}

/* ----- Contenido ----- */
.dash-content {
  flex: 1;
  padding: 2rem;
  display: flex;
  flex-direction: column;
  gap: 2rem;
}

/* ----- Estados (loading / empty / error) ----- */
.dash-state {
  padding: 3rem 2rem;
  text-align: center;
  color: var(--dash-text-2);
  font-size: 0.95rem;
}
.dash-state.dash-error { color: var(--dash-danger); }

/* ----- Métricas ----- */
.dash-metrics-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
  gap: 1.5rem;
}
.dash-metric-card {
  background: var(--dash-surface);
  border: 1px solid var(--dash-border);
  border-radius: 8px;
  padding: 1.5rem;
  transition: all 0.3s ease;
  position: relative;
  overflow: hidden;
}
.dash-metric-card::before {
  content: '';
  position: absolute;
  top: 0; left: 0; right: 0;
  height: 3px;
}
.dash-metric-card.dash-primary::before { background: var(--dash-primary); }
.dash-metric-card.dash-danger::before  { background: var(--dash-danger); }
.dash-metric-card.dash-warning::before { background: var(--dash-warning); }
.dash-metric-card.dash-success::before { background: var(--dash-success); }
.dash-metric-card:hover {
  border-color: var(--dash-primary);
  box-shadow: var(--dash-shadow-md);
  transform: translateY(-4px);
}
.dash-metric-label {
  font-size: 0.85rem;
  color: var(--dash-text-2);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 0.5rem;
  font-weight: 600;
}
.dash-metric-value {
  font-size: 2.5rem;
  font-weight: 700;
  color: var(--dash-text);
  margin-bottom: 0.5rem;
}
.dash-metric-subtitle {
  font-size: 0.8rem;
  color: var(--dash-text-2);
}

/* ----- Secciones ----- */
.dash-section-title {
  font-size: 0.8rem;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--dash-text-2);
  margin-bottom: 1.5rem;
  font-weight: 700;
}
.dash-projects-list {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}
.dash-project-row {
  background: var(--dash-surface);
  border: 1px solid var(--dash-border);
  border-radius: 8px;
  padding: 1.25rem;
  display: grid;
  grid-template-columns: 1fr auto;
  gap: 1.5rem;
  align-items: center;
  transition: all 0.3s ease;
}
.dash-project-row:hover {
  border-color: var(--dash-primary);
  background: var(--dash-surface-2);
  box-shadow: var(--dash-shadow-sm);
}
.dash-project-info { min-width: 0; }
.dash-project-name {
  font-size: 0.95rem;
  font-weight: 600;
  color: var(--dash-text);
  margin-bottom: 0.25rem;
}
.dash-project-date {
  font-size: 0.8rem;
  color: var(--dash-text-2);
}
.dash-project-stats {
  display: flex;
  gap: 1rem;
  align-items: center;
}
.dash-project-count {
  font-size: 0.85rem;
  color: var(--dash-text-2);
  white-space: nowrap;
}

/* ----- Badges ----- */
.dash-badge {
  display: inline-block;
  padding: 0.4rem 0.8rem;
  border-radius: 4px;
  font-size: 0.75rem;
  font-weight: 600;
  text-transform: uppercase;
  letter-spacing: 0.3px;
  white-space: nowrap;
}
.dash-badge-completada { background: rgba(46, 213, 115, 0.15); color: var(--dash-success); }
.dash-badge-encurso    { background: rgba(84, 160, 255, 0.15); color: var(--dash-info); }
.dash-badge-fallida    { background: rgba(255, 71, 87, 0.15);  color: var(--dash-danger); }
.dash-badge-nc  { background: rgba(255, 71, 87, 0.15);  color: var(--dash-danger); }
.dash-badge-obs { background: rgba(255, 165, 2, 0.15);  color: var(--dash-warning); }
.dash-badge-om  { background: rgba(84, 160, 255, 0.15); color: var(--dash-info); }

/* ----- Tabla de hallazgos ----- */
.dash-table-responsive {
  overflow-x: auto;
  border-radius: 8px;
  border: 1px solid var(--dash-border);
}
.dash-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 0.9rem;
}
.dash-table thead {
  background: var(--dash-surface-2);
  border-bottom: 2px solid var(--dash-border);
}
.dash-table th {
  padding: 1rem;
  text-align: left;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  font-size: 0.75rem;
  color: var(--dash-text-2);
}
.dash-table tbody tr {
  border-bottom: 1px solid var(--dash-border);
  transition: background 0.3s ease;
}
.dash-table tbody tr:last-child { border-bottom: none; }
.dash-table tbody tr:hover { background: var(--dash-surface-2); }
.dash-table td {
  padding: 1rem;
  color: var(--dash-text);
}
.dash-cell-desc { font-weight: 500; max-width: 360px; }
.dash-cell-proyecto { color: var(--dash-text-2); font-size: 0.9rem; }
.dash-cell-fecha { color: var(--dash-text-2); font-size: 0.9rem; text-align: right; }

/* ----- Responsive ----- */
@media (max-width: 1024px) {
  .dash-metrics-grid { grid-template-columns: repeat(2, 1fr); }
  .dash-project-row { grid-template-columns: 1fr; gap: 1rem; }
}
@media (max-width: 768px) {
  .dash-header { flex-direction: column; align-items: flex-start; gap: 1rem; }
  .dash-header-right { width: 100%; }
  .dash-metrics-grid { grid-template-columns: 1fr; }
  .dash-content { padding: 1rem; gap: 1.5rem; }
}
`;
