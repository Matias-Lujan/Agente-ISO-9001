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
  type SerieEvolucion,
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
const RANGO_DIAS: Record<Rango, number> = { '30': 30, trim: 90, todo: Infinity };

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
  // La barra se llena con la proporción que aporta cada agente sobre el total de hallazgos.
  const total = rows.reduce((s, r) => s + r.value, 0) || 1;
  return (
    <div className="dash-bars">
      {rows.map((b, i) => {
        const pct = Math.round((b.value / total) * 100);
        return (
          <div key={i} className="dash-bar-row ag">
            <span className="dash-bar-name" title={b.name}>{b.name}</span>
            <span className="dash-bar-track"><span className="dash-bar-fill acc" style={{ width: `${pct}%` }} /></span>
            <span className="dash-bar-val" title={`${pct}% del total`}>{b.value}</span>
          </div>
        );
      })}
    </div>
  );
}

function AreaEvolucion({ pts }: { pts: SerieEvolucion[] }) {
  if (pts.length === 0) return <div className="dash-empty">Todavía no hay hallazgos para graficar.</div>;
  // viewBox en escala ~px reales: la tipografía SVG queda al mismo tamaño que el resto de la pantalla.
  const W = 1480, H = 300, padL = 40, padR = 24, padT = 28, padB = 48;
  const innerW = W - padL - padR, innerH = H - padT - padB;
  const n = pts.length;
  const top = Math.max(...pts.map((p) => p.valor), 1) + 1; // +1 de aire para que el pico no quede pegado al borde
  const X = (i: number) => (n > 1 ? padL + i * (innerW / (n - 1)) : padL + innerW / 2);
  const Y = (v: number) => padT + (1 - v / top) * innerH;
  const baseY = padT + innerH;

  const vp = pts.map((p, i) => ({ ...p, vx: X(i), vy: Y(p.valor) }));
  const line = vp.map((p, k) => `${k ? 'L' : 'M'}${p.vx.toFixed(1)} ${p.vy.toFixed(1)}`).join(' ');
  const area = `${line} L ${vp[n - 1].vx.toFixed(1)} ${baseY.toFixed(1)} L ${vp[0].vx.toFixed(1)} ${baseY.toFixed(1)} Z`;
  // Eje Y entero: una marca por valor cuando hay pocos hallazgos, 0/mitad/máximo cuando hay muchos.
  const guias = top <= 5 ? Array.from({ length: top + 1 }, (_, i) => i) : [0, Math.round(top / 2), top];

  return (
    <div>
      <svg width="100%" viewBox={`0 0 ${W} ${H}`} className="dash-evo-svg">
        <defs>
          <linearGradient id="dashEvoGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor={COL.primary} stopOpacity="0.20" />
            <stop offset="100%" stopColor={COL.primary} stopOpacity="0" />
          </linearGradient>
        </defs>

        {/* Guías horizontales + eje Y (el valor de cada punto se lee acá, a la izquierda) */}
        {guias.map((g) => (
          <g key={g}>
            <line x1={padL} y1={Y(g)} x2={W - padR} y2={Y(g)} stroke={COL.track} strokeWidth="1" />
            <text x={padL - 10} y={Y(g) + 4} className="dash-evo-axis" textAnchor="end" fill={COL.muted}>{g}</text>
          </g>
        ))}

        {n > 1 && <path d={area} fill="url(#dashEvoGrad)" />}
        {n > 1 && <path d={line} className="dash-evo-line" fill="none" stroke={COL.primary} />}

        {vp.map((p) => (
          <g key={p.fecha}>
            <circle cx={p.vx} cy={p.vy} r="6" fill={COL.surface} stroke={COL.primary} strokeWidth="2.5" />
            <text x={p.vx} y={baseY + 26} className="dash-evo-x" textAnchor="middle" fill={COL.muted}>{p.etiqueta}</text>
          </g>
        ))}
      </svg>
      <div className="dash-evo-foot">
        {n === 1
          ? 'Hay un solo día con hallazgos. La curva se va a poblar a medida que registres auditorías en distintos días.'
          : 'Cada punto es un día con auditorías; la altura indica cuántos hallazgos se registraron ese día.'}
      </div>
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
    const dias = RANGO_DIAS[rango];
    if (dias === Infinity) return data.evolucion;
    const limite = Date.now() - dias * 24 * 60 * 60 * 1000;
    return data.evolucion.filter((p) => Date.parse(p.fecha) >= limite);
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
        const DR = 45, DC = 2 * Math.PI * DR;
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