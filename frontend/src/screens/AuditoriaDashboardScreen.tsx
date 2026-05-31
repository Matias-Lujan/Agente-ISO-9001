// ============================================================================
//  Pantalla: Dashboard del auditor.
//
//  Se monta dentro de ShellLayout (App.tsx), así que NO trae su propio sidebar.
//  Los datos vienen del backend vía cargarDashboard() (api/dashboard.ts), que
//  agrega proyectos + auditorías + hallazgos reales. La pantalla no sabe cómo
//  se arman: solo conoce cargarDashboard() y un DashboardData.
// ============================================================================

import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { dashboardCss } from '../styles/dashboard';
import { useAuth } from '../login/AuthContext';
import {
  cargarDashboard,
  type DashboardData,
  type EstadoAuditoria,
  type TipoHallazgo,
} from '../api/dashboard';

// ── Helpers de presentación ──────────────────────────────────────────────────

function formatFecha(iso: string): string {
  if (!iso) return '—';
  const ms = Date.parse(iso);
  if (Number.isNaN(ms)) return '—';
  return new Date(ms).toLocaleDateString('es-AR', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
  });
}

const ESTADO_BADGE: Record<EstadoAuditoria, { clase: string; label: string }> = {
  Completada: { clase: 'dash-badge-completada', label: 'Completada' },
  EnCurso:    { clase: 'dash-badge-encurso', label: 'En curso' },
  Fallida:    { clase: 'dash-badge-fallida', label: 'Fallida' },
};

const TIPO_BADGE: Record<TipoHallazgo, { clase: string; label: string }> = {
  NC:  { clase: 'dash-badge-nc', label: 'NC' },
  OBS: { clase: 'dash-badge-obs', label: 'OBS' },
  OM:  { clase: 'dash-badge-om', label: 'OM' },
};

// ── Componente ───────────────────────────────────────────────────────────────

export default function AuditoriaDashboardScreen() {
  useInjectStyle(dashboardCss, 'dashboard-style');

  const navigate = useNavigate();
  const { usuario } = useAuth();

  const [isDark, setIsDark] = useState(false);
  const [data, setData] = useState<DashboardData | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let activo = true;
    setCargando(true);
    setError(null);

    cargarDashboard()
      .then((d) => {
        if (activo) setData(d);
      })
      .catch((e: unknown) => {
        if (activo) setError(e instanceof Error ? e.message : 'No se pudo cargar el dashboard');
      })
      .finally(() => {
        if (activo) setCargando(false);
      });

    return () => {
      activo = false;
    };
  }, []);

  const m = data?.metricas;

  return (
    <div className={`dash-root${isDark ? ' dash-dark' : ''}`}>
      {/* HEADER */}
      <header className="dash-header">
        <div className="dash-header-left">
          <h1 className="dash-title">Dashboard del auditor</h1>
          <p className="dash-subtitle">
            {usuario?.nombre
              ? `Hola ${usuario.nombre} — resumen de auditorías y estado de proyectos`
              : 'Resumen de auditorías y estado de proyectos'}
          </p>
        </div>
        <div className="dash-header-right">
          <button className="dash-btn-theme" onClick={() => setIsDark((v) => !v)}>
            {isDark ? '☀️ Modo claro' : '🌙 Modo oscuro'}
          </button>
          <button className="dash-btn-new" onClick={() => navigate('/nueva-auditoria')}>
            ➕ Nueva auditoría
          </button>
        </div>
      </header>

      {/* ESTADOS */}
      {cargando && <div className="dash-state">Cargando dashboard…</div>}
      {error && !cargando && (
        <div className="dash-state dash-error">⚠️ {error}</div>
      )}

      {/* CONTENIDO */}
      {!cargando && !error && data && m && (
        <div className="dash-content">
          {/* MÉTRICAS */}
          <section>
            <div className="dash-metrics-grid">
              <MetricCard
                color="dash-primary"
                label="Proyectos auditados"
                value={m.proyectosAuditados}
                subtitle={`${m.proyectosTotal} en total`}
              />
              <MetricCard
                color="dash-danger"
                label="No conformidades"
                value={m.noConformidades}
                subtitle={`${m.totalHallazgos} hallazgos en total`}
              />
              <MetricCard
                color="dash-warning"
                label="Observaciones"
                value={m.observaciones}
                subtitle={`${m.oportunidadesMejora} oportunidades de mejora`}
              />
              <MetricCard
                color="dash-success"
                label="Auditorías completadas"
                value={m.auditoriasCompletadas}
                subtitle={`de ${m.auditoriasTotal} auditorías`}
              />
            </div>
          </section>

          {/* PROYECTOS RECIENTES */}
          <section>
            <h2 className="dash-section-title">Proyectos recientes</h2>
            {data.proyectosRecientes.length === 0 ? (
              <div className="dash-state">Todavía no hay auditorías registradas.</div>
            ) : (
              <div className="dash-projects-list">
                {data.proyectosRecientes.map((p) => {
                  const badge = ESTADO_BADGE[p.estado];
                  return (
                    <div key={p.auditoriaId} className="dash-project-row">
                      <div className="dash-project-info">
                        <h3 className="dash-project-name">{p.proyecto}</h3>
                        <p className="dash-project-date">Auditado el {formatFecha(p.fecha)}</p>
                      </div>
                      <div className="dash-project-stats">
                        <span className="dash-project-count">
                          {p.hallazgosTotal} hallazgos
                          {p.hallazgosNC > 0 ? ` · ${p.hallazgosNC} NC` : ''}
                        </span>
                        <span className={`dash-badge ${badge.clase}`}>{badge.label}</span>
                      </div>
                    </div>
                  );
                })}
              </div>
            )}
          </section>

          {/* ÚLTIMOS HALLAZGOS */}
          <section>
            <h2 className="dash-section-title">Últimos hallazgos</h2>
            {data.ultimosHallazgos.length === 0 ? (
              <div className="dash-state">No se registraron hallazgos.</div>
            ) : (
              <div className="dash-table-responsive">
                <table className="dash-table">
                  <thead>
                    <tr>
                      <th>Descripción</th>
                      <th>Proyecto</th>
                      <th>Tipo</th>
                      <th>Fecha</th>
                    </tr>
                  </thead>
                  <tbody>
                    {data.ultimosHallazgos.map((h) => {
                      const badge = TIPO_BADGE[h.tipo];
                      return (
                        <tr key={h.id}>
                          <td className="dash-cell-desc">{h.descripcion}</td>
                          <td className="dash-cell-proyecto">{h.proyecto}</td>
                          <td>
                            <span className={`dash-badge ${badge.clase}`}>{badge.label}</span>
                          </td>
                          <td className="dash-cell-fecha">{formatFecha(h.fecha)}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      )}
    </div>
  );
}

// ── Subcomponente de tarjeta de métrica ──────────────────────────────────────

function MetricCard({
  color,
  label,
  value,
  subtitle,
}: {
  color: 'dash-primary' | 'dash-danger' | 'dash-warning' | 'dash-success';
  label: string;
  value: number;
  subtitle: string;
}) {
  return (
    <div className={`dash-metric-card ${color}`}>
      <p className="dash-metric-label">{label}</p>
      <p className="dash-metric-value">{value}</p>
      <p className="dash-metric-subtitle">{subtitle}</p>
    </div>
  );
}
