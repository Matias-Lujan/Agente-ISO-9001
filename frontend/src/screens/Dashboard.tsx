// ============================================================================
//  Pantalla: Dashboard del auditor.
//
//  Se monta dentro de ShellLayout (App.tsx) → NO trae sidebar propio.
//  Estilo alineado con el prototipo y el resto de pantallas: reutiliza las
//  clases del sharedCss (topbar / page-title / page-sub / btn-pri /
//  loading-text / error-banner) y agrega las propias (dash-*) en dashboardCss.
//
//  Los datos vienen del backend vía cargarDashboard() (api/dashboard.ts).
//  Sin modo oscuro: el toggle se moverá a Configuración más adelante.
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
import { cargarRevision, type Revision } from '../api/hallazgos';

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
  Completada: { clase: 'dash-badge-ok', label: 'Completada' },
  EnCurso:    { clase: 'dash-badge-pen', label: 'En curso' },
  Fallida:    { clase: 'dash-badge-nc', label: 'Fallida' },
};

const TIPO_BADGE: Record<TipoHallazgo, { clase: string; label: string }> = {
  NC:  { clase: 'dash-badge-nc', label: 'No conformidad' },
  OBS: { clase: 'dash-badge-pen', label: 'Observación' },
  OM:  { clase: 'dash-badge-om', label: 'Oportunidad' },
};

const DASH_R = 46;
const DASH_C = 2 * Math.PI * DASH_R;

// ── Componente ───────────────────────────────────────────────────────────────

export default function Dashboard() {
  useInjectStyle(dashboardCss, 'dashboard-style');

  const navigate = useNavigate();
  const { usuario } = useAuth();

  const [data, setData] = useState<DashboardData | null>(null);
  const [revision, setRevision] = useState<Revision | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let activo = true;
    setCargando(true);
    setError(null);

    cargarDashboard()
      .then((d) => { if (activo) setData(d); })
      .catch((e: unknown) => {
        if (activo) setError(e instanceof Error ? e.message : 'No se pudo cargar el dashboard');
      })
      .finally(() => { if (activo) setCargando(false); });

    // Cumplimiento general: mismos números que la vista de Hallazgos.
    cargarRevision()
      .then((r) => { if (activo) setRevision(r); })
      .catch(() => { /* el resto del dashboard no depende de esto */ });

    return () => { activo = false; };
  }, []);

  const m = data?.metricas;

  return (
    <>
      <div className="topbar">
        <div>
          <div className="page-title">Dashboard del auditor</div>
          <div className="page-sub">
            {usuario?.nombre
              ? `Hola ${usuario.nombre} — resumen de auditorías y estado de proyectos`
              : 'Resumen de auditorías y estado de proyectos'}
          </div>
        </div>
        <button className="btn-pri" onClick={() => navigate('/nueva-auditoria')}>
          + Nueva auditoría
        </button>
      </div>

      {cargando && <div className="loading-text">Cargando dashboard…</div>}
      {error && !cargando && <div className="error-banner">{error}</div>}

      {!cargando && !error && data && m && (
        <>
          {/* MÉTRICAS */}
          <div className="dash-metrics">
            <div className="dash-metric">
              <div className="dash-metric-label">Proyectos auditados</div>
              <div className="dash-metric-value">{m.proyectosAuditados}</div>
              <div className="dash-metric-sub">{m.proyectosTotal} en total</div>
            </div>
            <div className="dash-metric">
              <div className="dash-metric-label">No conformidades</div>
              <div className="dash-metric-value dash-v-nc">{m.noConformidades}</div>
              <div className="dash-metric-sub">{m.totalHallazgos} hallazgos en total</div>
            </div>
            <div className="dash-metric">
              <div className="dash-metric-label">Observaciones</div>
              <div className="dash-metric-value dash-v-warn">{m.observaciones}</div>
              <div className="dash-metric-sub">{m.oportunidadesMejora} oportunidades de mejora</div>
            </div>
            <div className="dash-metric">
              <div className="dash-metric-label">Auditorías completadas</div>
              <div className="dash-metric-value dash-v-ok">{m.auditoriasCompletadas}</div>
              <div className="dash-metric-sub">de {m.auditoriasTotal} auditorías</div>
            </div>
          </div>

          {/* CUMPLIMIENTO GENERAL */}
          <div className="dash-section-title">Cumplimiento general</div>
          <div className="dash-cumpl">
            {!revision ? (
              <div className="dash-empty">Calculando cumplimiento…</div>
            ) : (() => {
              const nc = revision.hallazgos.filter((h) => h.tipo === 'NoConformidad').length;
              const sem = nc > 0 ? 'rojo' : revision.hallazgos.length > 0 ? 'amarillo' : 'verde';
              const estado = nc > 0 ? 'Requiere atención' : revision.hallazgos.length > 0 ? 'Con observaciones' : 'Conforme';
              const pct = revision.revisados > 0 ? Math.round((revision.totalConformes / revision.revisados) * 100) : 0;
              const segConf = revision.revisados > 0 ? (revision.totalConformes / revision.revisados) * DASH_C : 0;
              return (
                <>
                  <div className={`dash-sem ${sem}`}>
                    <div className="dash-light-stack"><span className="dash-lt r" /><span className="dash-lt y" /><span className="dash-lt g" /></div>
                    <div>
                      <div className="dash-sem-estado">{estado}</div>
                      <div className="dash-sem-resumen">
                        Se revisaron {revision.revisados} ítems: {revision.totalConformes} conformes · {revision.hallazgos.length} con hallazgos
                        {nc > 0 ? ` (incluye ${nc} no conformidad${nc > 1 ? 'es' : ''}).` : revision.hallazgos.length > 0 ? ' (sin no conformidades).' : '. Todo en orden.'}
                      </div>
                    </div>
                  </div>

                  <div className="dash-donut">
                    <svg width="112" height="112" viewBox="0 0 112 112">
                      <circle className="track" cx="56" cy="56" r={DASH_R} fill="none" strokeWidth="14" />
                      {revision.revisados > 0 && (
                        <>
                          <circle cx="56" cy="56" r={DASH_R} fill="none" stroke="var(--ok-fg)" strokeWidth="14"
                            strokeDasharray={`${segConf.toFixed(2)} ${(DASH_C - segConf).toFixed(2)}`} strokeDashoffset="0" />
                          <circle cx="56" cy="56" r={DASH_R} fill="none" stroke="var(--err-fg)" strokeWidth="14"
                            strokeDasharray={`${(DASH_C - segConf).toFixed(2)} ${segConf.toFixed(2)}`} strokeDashoffset={`${(-segConf).toFixed(2)}`} />
                        </>
                      )}
                    </svg>
                    <div className="dash-donut-center"><div className="pct">{pct}%</div><div className="cap">cumplimiento</div></div>
                  </div>

                  <div className="dash-dleg">
                    <div className="l"><span className="sw" style={{ background: 'var(--ok-fg)' }} />Conforme <b>{revision.totalConformes}</b></div>
                    <div className="l"><span className="sw" style={{ background: 'var(--err-fg)' }} />No conforme <b>{revision.totalNoConformes}</b></div>
                  </div>
                </>
              );
            })()}
          </div>

          {/* PROYECTOS RECIENTES */}
          <div className="dash-section-title">Proyectos recientes</div>
          <div className="dash-projects">
            {data.proyectosRecientes.length === 0 ? (
              <div className="dash-empty">Todavía no hay auditorías registradas.</div>
            ) : (
              data.proyectosRecientes.map((p) => {
                const badge = ESTADO_BADGE[p.estado];
                return (
                  <div key={p.auditoriaId} className="dash-project-row">
                    <div style={{ flex: 1 }}>
                      <div className="dash-proj-name">{p.proyecto}</div>
                      <div className="dash-proj-date">Auditado el {formatFecha(p.fecha)}</div>
                    </div>
                    <span className="dash-proj-count">
                      {p.hallazgosTotal} hallazgos{p.hallazgosNC > 0 ? ` · ${p.hallazgosNC} NC` : ''}
                    </span>
                    <span className={`dash-badge ${badge.clase}`}>{badge.label}</span>
                  </div>
                );
              })
            )}
          </div>

          {/* ÚLTIMOS HALLAZGOS */}
          <div className="dash-section-title">Últimos hallazgos</div>
          <div className="dash-hallazgos">
            <div className="dash-hall-header">
              <div>Descripción</div><div>Proyecto</div><div>Tipo</div><div>Fecha</div>
            </div>
            {data.ultimosHallazgos.length === 0 ? (
              <div className="dash-empty">No se registraron hallazgos.</div>
            ) : (
              data.ultimosHallazgos.map((h) => {
                const badge = TIPO_BADGE[h.tipo];
                return (
                  <div key={h.id} className="dash-hall-row">
                    <div>{h.descripcion}</div>
                    <div className="dash-hall-proj">{h.proyecto}</div>
                    <div><span className={`dash-badge ${badge.clase}`}>{badge.label}</span></div>
                    <div className="dash-hall-date">{formatFecha(h.fecha)}</div>
                  </div>
                );
              })
            )}
          </div>
        </>
      )}
    </>
  );
}