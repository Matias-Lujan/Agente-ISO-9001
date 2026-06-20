// ============================================================================
//  Pantalla: Dashboard del auditor (solo Administrador / Auditor).
//
//  Se monta dentro de ShellLayout (App.tsx) → NO trae sidebar propio.
//  Reutiliza el sharedCss (topbar / page-title / btn-pri / etc.) y las clases
//  dash-* de dashboardCss. Sin selector de rol (el dashboard solo lo ven Admin
//  y Auditor, que ven la misma vista global).
//
//  Toda la data sale de cargarDashboardCompleto() (api/dashboard.ts), derivada
//  de los endpoints reales. Gráficos en SVG/CSS (sin dependencias extra).
// ============================================================================

import { useEffect, useMemo, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { dashboardCss } from '../styles/dashboard';
import { exportarDashboardPdf } from '../utils/exportDashboardPdf';
import { useAuth } from '../login/AuthContext';
import {
  cargarDashboardCompleto,
  type DashboardCompleto,
  type ProyectoCumpl,
  type SerieMes,
  type TipoHallazgo,
} from '../api/dashboard';

// ── Helpers ──────────────────────────────────────────────────────────────────

function formatFecha(iso: string): string {
  const ms = Date.parse(iso);
  if (Number.isNaN(ms)) return '—';
  return new Date(ms).toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit', year: 'numeric' });
}

const TIPO_BADGE: Record<TipoHallazgo, { clase: string; label: string }> = {
  NC:  { clase: 'dash-badge-nc', label: 'No conformidad' },
  OBS: { clase: 'dash-badge-pen', label: 'Observación' },
  OM:  { clase: 'dash-badge-om', label: 'Oportunidad' },
};

const claseCumpl = (p: number) => (p >= 90 ? 'ok' : p >= 70 ? 'warn' : 'err');

// Colores literales para los SVG: html2canvas renderiza el SVG aislado y no
// resuelve var(--…). El dashboard es solo tema claro, así que usamos los hex.
const COL = {
  ok: '#3a8a1e', err: '#c23a3a', warn: '#c98a12', info: '#4b2dab',
  primary: '#4b2dab', track: '#eeecf3', surface: '#ffffff', muted: '#7b6aaa',
};

type Rango = '30' | 'trim' | 'todo';
const RANGO_LABEL: Record<Rango, string> = { '30': 'Últimos 30 días', trim: 'Último trimestre', todo: 'Todo el período' };
const RANGO_N: Record<Rango, number> = { '30': 2, trim: 3, todo: Infinity };

// ── Sub-componentes de gráficos ──────────────────────────────────────────────

interface Segmento { label: string; value: number; color: string; }

function Donut({ data, cap }: { data: Segmento[]; cap: string }) {
  const total = data.reduce((s, d) => s + d.value, 0);
  const r = 58, C = 2 * Math.PI * r;
  let acc = 0;
  return (
    <div className="dash-dwrap">
      <div className="dash-dchart">
        <svg width="150" height="150" viewBox="0 0 150 150">
          <circle className="track" cx="75" cy="75" r={r} fill="none" stroke={COL.track} strokeWidth="20" />
          {data.filter((d) => d.value > 0).map((d, i) => {
            const len = (d.value / total) * C;
            const off = -acc;
            acc += len;
            return (
              <circle key={i} cx="75" cy="75" r={r} fill="none" stroke={d.color} strokeWidth="20"
                strokeDasharray={`${len.toFixed(2)} ${(C - len).toFixed(2)}`} strokeDashoffset={off.toFixed(2)} />
            );
          })}
        </svg>
        <div className="center"><div className="tot">{total}</div><div className="cap">{cap}</div></div>
      </div>
      <div className="dash-legend">
        {data.map((d, i) => (
          <div key={i} className="dash-leg">
            <span className="sw" style={{ background: d.color }} />
            <span className="lab">{d.label}</span>
            <span className="val">{d.value}</span>
            <span className="pct">{total ? Math.round((d.value / total) * 100) : 0}%</span>
          </div>
        ))}
      </div>
    </div>
  );
}

function BarsCumpl({ rows }: { rows: { name: string; pct: number }[] }) {
  if (rows.length === 0) return <div className="dash-empty">Sin proyectos con auditoría completada.</div>;
  return (
    <>
      <div className="dash-bars">
        {rows.map((b, i) => (
          <div key={i} className="dash-bar-row">
            <span className="dash-bar-name" title={b.name}>{b.name}</span>
            <span className="dash-bar-track"><span className={`dash-bar-fill ${claseCumpl(b.pct)}`} style={{ width: `${b.pct}%` }} /></span>
            <span className="dash-bar-val" style={{ color: `var(--${claseCumpl(b.pct)}-fg)` }}>{b.pct}%</span>
          </div>
        ))}
      </div>
      <div className="dash-bar-note">
        <span><i style={{ background: 'var(--ok-fg)' }} />≥ 90%</span>
        <span><i style={{ background: 'var(--warn-fg)' }} />70–89%</span>
        <span><i style={{ background: 'var(--err-fg)' }} />&lt; 70%</span>
      </div>
    </>
  );
}

function BarsAgente({ rows }: { rows: { name: string; value: number }[] }) {
  if (rows.length === 0) return <div className="dash-empty">Sin hallazgos registrados.</div>;
  const max = Math.max(...rows.map((r) => r.value), 1);
  return (
    <div className="dash-bars">
      {rows.map((b, i) => (
        <div key={i} className="dash-bar-row ag">
          <span className="dash-bar-name" title={b.name}>{b.name}</span>
          <span className="dash-bar-track"><span className="dash-bar-fill acc" style={{ width: `${Math.round((b.value / max) * 100)}%` }} /></span>
          <span className="dash-bar-val">{b.value}</span>
        </div>
      ))}
    </div>
  );
}

function AreaEvolucion({ pts }: { pts: SerieMes[] }) {
  if (pts.length === 0) return <div className="dash-empty">Todavía no hay hallazgos para graficar.</div>;
  const W = 640, H = 150, pad = 10;
  const max = Math.max(...pts.map((p) => p.valor), 1);
  const step = pts.length > 1 ? (W - pad * 2) / (pts.length - 1) : 0;
  const xy = pts.map((p, i) => [pad + i * step, H - pad - (p.valor / max) * (H - pad * 2)] as const);
  const line = xy.map((p, i) => `${i ? 'L' : 'M'}${p[0].toFixed(1)} ${p[1].toFixed(1)}`).join(' ');
  const area = `${line} L ${xy[xy.length - 1][0].toFixed(1)} ${H - pad} L ${xy[0][0].toFixed(1)} ${H - pad} Z`;
  return (
    <div>
      <svg width="100%" viewBox={`0 0 ${W} ${H}`} preserveAspectRatio="none" style={{ display: 'block' }}>
        {pts.length > 1 && <path d={area} fill="rgba(75,45,171,0.15)" />}
        {pts.length > 1 && <path d={line} fill="none" stroke={COL.primary} strokeWidth="2.5" />}
        {xy.map((p, i) => (
          <circle key={i} cx={p[0].toFixed(1)} cy={p[1].toFixed(1)} r="3.5" fill={COL.surface} stroke={COL.primary} strokeWidth="2" />
        ))}
      </svg>
      <div className="dash-area-x">{pts.map((p, i) => <span key={i}>{p.etiqueta}</span>)}</div>
    </div>
  );
}

// ── Skeleton ─────────────────────────────────────────────────────────────────

function Skeleton() {
  const line = (w: string, h = 12) => <div className="dash-sk dash-sk-line" style={{ width: w, height: h }} />;
  return (
    <>
      <div className="dash-metrics">
        {[0, 1, 2, 3].map((i) => (
          <div key={i} className="dash-metric">{line('50%', 24)}{line('70%')}</div>
        ))}
      </div>
      <div className="dash-card"><div className="dash-dwrap"><div className="dash-sk dash-sk-donut" /><div className="dash-legend" style={{ flex: 1 }}>{line('90%')}{line('70%')}{line('80%')}</div></div></div>
      <div className="dash-grid2">
        <div className="dash-card">{line('60%')}{line('90%')}{line('75%')}</div>
        <div className="dash-card">{line('60%')}{line('90%')}{line('75%')}</div>
      </div>
    </>
  );
}

// ── Componente principal ─────────────────────────────────────────────────────

export default function Dashboard() {
  useInjectStyle(dashboardCss, 'dashboard-style');
  const navigate = useNavigate();
  const { usuario } = useAuth();

  const [data, setData] = useState<DashboardCompleto | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [rango, setRango] = useState<Rango>('todo');
  const [exportando, setExportando] = useState(false);
  const pdfRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    let activo = true;
    setCargando(true); setError(null);
    cargarDashboardCompleto()
      .then((d) => { if (activo) setData(d); })
      .catch((e: unknown) => { if (activo) setError(e instanceof Error ? e.message : 'No se pudo cargar el dashboard'); })
      .finally(() => { if (activo) setCargando(false); });
    return () => { activo = false; };
  }, []);

  const evolucionVisible = useMemo(() => {
    if (!data) return [];
    const n = RANGO_N[rango];
    return n === Infinity ? data.evolucion : data.evolucion.slice(Math.max(0, data.evolucion.length - n));
  }, [data, rango]);

  const irAInforme = (p: ProyectoCumpl) =>
    navigate(`/informes?proyecto=${encodeURIComponent(p.nombre)}`);

  const handleExport = async () => {
    if (!pdfRef.current) return;
    setExportando(true);
    try {
      await exportarDashboardPdf(pdfRef.current, usuario?.nombre ?? 'Usuario');
    } catch (e) {
      console.error('Error al exportar el dashboard:', e);
      alert('Hubo un error al generar el PDF del dashboard.');
    } finally {
      setExportando(false);
    }
  };

  return (
    <>
      <div className="topbar">
        <div>
          <div className="page-title">Dashboard del auditor</div>
          <div className="page-sub">
            {usuario?.nombre ? `Hola ${usuario.nombre} — resumen de auditorías y estado de proyectos` : 'Resumen de auditorías y estado de proyectos'}
          </div>
        </div>
        <div style={{ display: 'flex', gap: '8px' }}>
          <button className="btn-ghost" onClick={handleExport} disabled={exportando || cargando || !!error || !data}>
            {exportando ? 'Generando…' : 'Exportar PDF'}
            {!exportando && (
              <svg width="13" height="13" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" style={{ marginLeft: 6 }}><path d="M12 3v12M7 10l5 5 5-5M5 21h14" /></svg>
            )}
          </button>
          <button className="btn-pri" onClick={() => navigate('/nueva-auditoria')}>+ Nueva auditoría</button>
        </div>
      </div>

      {error && !cargando && <div className="error-banner">{error}</div>}
      {cargando && !error && <Skeleton />}

      {!cargando && !error && data && (() => {
        const m = data.metricas;
        const cg = data.cumplimientoGeneral;
        const ht = data.hallazgosPorTipo;
        const pe = data.proyectosPorEstado;
        const nc = ht.nc;
        const sem = nc > 0 ? 'rojo' : (ht.obs + ht.om) > 0 ? 'amarillo' : 'verde';
        const estado = nc > 0 ? 'Requiere atención' : (ht.obs + ht.om) > 0 ? 'Con observaciones' : 'Conforme';
        const totalHz = ht.nc + ht.obs + ht.om;
        const DR = 56, DC = 2 * Math.PI * DR;
        const segConf = cg.revisados > 0 ? (cg.conformes / cg.revisados) * DC : 0;

        return (
          <div ref={pdfRef}>
            {/* MÉTRICAS */}
            <div className="dash-metrics dash-pdf-block">
              <div className="dash-metric"><div className="dash-metric-label">Proyectos auditados</div><div className="dash-metric-value">{m.proyectosAuditados}</div><div className="dash-metric-sub">{m.proyectosTotal} en total</div></div>
              <div className="dash-metric"><div className="dash-metric-label">No conformidades</div><div className="dash-metric-value dash-v-nc">{m.noConformidades}</div><div className="dash-metric-sub">{m.totalHallazgos} hallazgos en total</div></div>
              <div className="dash-metric"><div className="dash-metric-label">Observaciones</div><div className="dash-metric-value dash-v-warn">{m.observaciones}</div><div className="dash-metric-sub">{m.oportunidadesMejora} oportunidades de mejora</div></div>
              <div className="dash-metric"><div className="dash-metric-label">Auditorías completadas</div><div className="dash-metric-value dash-v-ok">{m.auditoriasCompletadas}</div><div className="dash-metric-sub">de {m.auditoriasTotal} auditorías</div></div>
            </div>

            {/* CUMPLIMIENTO GENERAL */}
            <div className="dash-pdf-block">
            <div className="dash-section-title">Cumplimiento general</div>
            <div className="dash-cumpl">
              <div className={`dash-sem ${sem}`}>
                <div className="dash-light-stack"><span className="dash-lt r" /><span className="dash-lt y" /><span className="dash-lt g" /></div>
                <div>
                  <div className="dash-sem-estado">{estado}</div>
                  <div className="dash-sem-resumen">
                    Se revisaron {cg.revisados} ítems: {cg.conformes} conformes · {totalHz} con hallazgos
                    {nc > 0 ? ` (incluye ${nc} no conformidad${nc > 1 ? 'es' : ''}).` : totalHz > 0 ? ' (sin no conformidades).' : '. Todo en orden.'}
                  </div>
                </div>
              </div>
              <div className="dash-donut">
                <svg width="112" height="112" viewBox="0 0 112 112">
                  <circle className="track" cx="56" cy="56" r={DR} fill="none" stroke={COL.track} strokeWidth="14" />
                  {cg.revisados > 0 && (<>
                    <circle cx="56" cy="56" r={DR} fill="none" stroke={COL.ok} strokeWidth="14" strokeDasharray={`${segConf.toFixed(2)} ${(DC - segConf).toFixed(2)}`} strokeDashoffset="0" />
                    <circle cx="56" cy="56" r={DR} fill="none" stroke={COL.err} strokeWidth="14" strokeDasharray={`${(DC - segConf).toFixed(2)} ${segConf.toFixed(2)}`} strokeDashoffset={`${(-segConf).toFixed(2)}`} />
                  </>)}
                </svg>
                <div className="dash-donut-center"><div className="pct">{cg.pct}%</div><div className="cap">cumplimiento</div></div>
              </div>
              <div className="dash-dleg">
                <div className="l"><span className="sw" style={{ background: 'var(--ok-fg)' }} />Conforme <b>{cg.conformes}</b></div>
                <div className="l"><span className="sw" style={{ background: 'var(--err-fg)' }} />No conforme <b>{cg.noConformes}</b></div>
              </div>
            </div>
            </div>

            {/* DONUTS: por tipo + por estado */}
            <div className="dash-grid2 dash-pdf-block">
              <div className="dash-card">
                <h2>Hallazgos por tipo</h2>
                <Donut cap="hallazgos" data={[
                  { label: 'No conformidad', value: ht.nc, color: COL.err },
                  { label: 'Observación', value: ht.obs, color: COL.warn },
                  { label: 'Oportunidad de mejora', value: ht.om, color: COL.info },
                ]} />
              </div>
              <div className="dash-card">
                <h2>Proyectos por estado</h2>
                <Donut cap="proyectos" data={[
                  { label: 'Completada', value: pe.completada, color: COL.ok },
                  { label: 'En curso', value: pe.enCurso, color: COL.warn },
                  { label: 'Fallida', value: pe.fallida, color: COL.err },
                  { label: 'Sin auditar', value: pe.sinAuditar, color: COL.muted },
                ]} />
              </div>
            </div>

            {/* CUMPLIMIENTO POR PROYECTO */}
            <div className="dash-card dash-pdf-block">
              <h2>Cumplimiento % por proyecto</h2>
              <BarsCumpl rows={data.cumplPorProyecto.filter((p) => p.cumpl != null).map((p) => ({ name: p.nombre, pct: p.cumpl as number }))} />
            </div>

            {/* EVOLUCIÓN */}
            <div className="dash-card dash-pdf-block">
              <h2>Evolución de hallazgos
                <span className="dash-controls"><span className="dash-seg">
                  {(['30', 'trim', 'todo'] as Rango[]).map((r) => (
                    <button key={r} className={rango === r ? 'on' : ''} onClick={() => setRango(r)}>
                      {r === '30' ? '30 días' : r === 'trim' ? 'Trimestre' : 'Todo'}
                    </button>
                  ))}
                </span></span>
              </h2>
              <AreaEvolucion pts={evolucionVisible} />
              <div className="dash-bar-note"><span style={{ color: 'var(--text-muted)' }}>{RANGO_LABEL[rango]}</span></div>
            </div>

            {/* POR AGENTE + POR PROCEDIMIENTO */}
            <div className="dash-grid2 dash-pdf-block">
              <div className="dash-card">
                <h2>Hallazgos por agente</h2>
                <BarsAgente rows={data.hallazgosPorAgente.map((a) => ({ name: a.agente, value: a.cantidad }))} />
              </div>
              <div className="dash-card">
                <h2>Cumplimiento por procedimiento</h2>
                <BarsCumpl rows={data.cumplPorProcedimiento.map((c) => ({ name: c.procedimiento, pct: c.pct }))} />
              </div>
            </div>

            {/* ATENCIÓN */}
            <div className="dash-card dash-pdf-block">
              <h2>Proyectos que requieren atención</h2>
              {data.atencion.length === 0 ? (
                <div className="dash-empty">No hay proyectos que requieran atención. 🎉</div>
              ) : (
                data.atencion.map((p) => {
                  const motivo = p.nc > 0 ? `${p.nc} NC sin resolver` : 'cumplimiento bajo';
                  const cls = p.cumpl == null ? 'pen' : p.cumpl >= 90 ? 'ok' : p.cumpl >= 70 ? 'pen' : 'nc';
                  return (
                    <div key={p.id} className="dash-att-row" onClick={() => irAInforme(p)} title={`Ver informes de ${p.nombre}`}>
                      <div className="dash-att-name">{p.nombre}<small>{p.procCodigo} · {motivo}</small></div>
                      <span className={`dash-badge dash-badge-${cls}`}>{p.cumpl == null ? '—' : `${p.cumpl}%`}</span>
                      <span className={`dash-badge ${p.nc > 0 ? 'dash-badge-nc' : 'dash-badge-ok'}`}>{p.nc} NC</span>
                      <span className="dash-att-go"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m9 6 6 6-6 6" /></svg></span>
                    </div>
                  );
                })
              )}
            </div>

            {/* ÚLTIMOS HALLAZGOS */}
            <div className="dash-pdf-block">
            <div className="dash-section-title">Últimos hallazgos</div>
            <div className="dash-hallazgos">
              <div className="dash-hall-header"><div>Descripción</div><div>Proyecto</div><div>Tipo</div><div>Fecha</div></div>
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
            </div>
          </div>
        );
      })()}
    </>
  );
}