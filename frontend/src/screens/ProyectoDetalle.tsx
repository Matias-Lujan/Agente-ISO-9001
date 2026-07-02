// src/screens/ProyectoDetalle.tsx
// ============================================================================
//  Detalle de un proyecto (ruta /proyectos/:id). Todo data real:
//   - Proyecto:   GET /api/proyectos/{id}
//   - Auditorías: GET /api/auditorias/proyecto/{id}
//   - Resultado:  GET /api/auditorias/{auditoriaId}/resultado  (hallazgos,
//                 artefactos evaluados, documentos analizados)
//   - Informes:   GET /api/informes/auditoria/{auditoriaId}
//   - Etapas:     GET /api/procedimientos/{id}/etapas  (para nombrar la etapa)
// ============================================================================
import { useEffect, useMemo, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { proyectoDetalleCss } from '../styles/proyectoDetalle';
import { descargarInformePdf, previsualizarInformePdf } from '../utils/exportInformePdf';
import { obtenerProyecto, type Proyecto } from '../api/proyectos';
import {
  listarAuditoriasDeProyecto,
  obtenerErrores,
  obtenerResultado,
  type AuditoriaDeProyecto,
  type AuditoriaResultado,
  type NodoWorkflow,
  type RegistroError,
} from '../api/auditorias';
import { listarInformesDeAuditoria, TIPO_INFORME_LABEL, type Informe } from '../api/informes';
import { listarProcedimientos, listarEtapasDeProcedimiento, type Etapa } from '../api/procedimientos';

const COLOR_INT: Record<string, string> = { drive: '#1FA463', trello: '#0079BF', clockify: '#E5704B' };

function ini(n: string): string {
  return n.trim().split(/\s+/).map((p) => p[0]).slice(0, 2).join('').toUpperCase();
}
function fmt(iso: string | null): string {
  if (!iso) return '—';
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('es-AR');
}

const ESTADO_AUD: Record<string, string> = {
  Completada: 'b-ok', EnCurso: 'b-warn', Fallida: 'b-err',
};

// Etiqueta humana de un nodo del workflow para el log de errores. Los errores
// fuera de nodo llegan con nodo === null.
const NODO_LABEL: Record<NodoWorkflow, string> = {
  DocumentAnalysis: 'Análisis documental',
  ComplianceValidation: 'Validación de procesos',
  ConsistencyVerification: 'Verificación de consistencia',
  FindingsClassification: 'Clasificación de hallazgos',
};
function labelNodo(nodo: NodoWorkflow | null): string {
  return nodo === null ? 'Preparación de la auditoría' : NODO_LABEL[nodo] ?? nodo;
}
const RESULTADO_BADGE: Record<string, string> = {
  Conforme: 'b-ok', NoConforme: 'b-err', NoAplica: 'b-gray', PendienteEtapaFutura: 'b-warn',
};
const RESULTADO_LABEL: Record<string, string> = {
  Conforme: 'Conforme', NoConforme: 'No conforme', NoAplica: 'No aplica', PendienteEtapaFutura: 'Pendiente',
};
const APLICA_LABEL: Record<string, string> = {
  Aplica: 'Aplica', NoAplica: 'No aplica', SinDeclararEnTailoring: 'No declarado en tailoring',
};

// % cumplimiento = conformes / (conformes + no conformes) entre los aplicables.
function calcular(res: AuditoriaResultado | null) {
  if (!res) return null;
  const aplic = res.artefactosEvaluados.filter((a) => a.aplica === 'Aplica');
  const conf = aplic.filter((a) => a.resultado === 'Conforme').length;
  const nc = aplic.filter((a) => a.resultado === 'NoConforme').length;
  const ev = conf + nc;
  if (ev === 0) return null;
  return { pct: Math.round((conf / ev) * 100), conf, ev };
}

const R = 26;
const C = 2 * Math.PI * R;

// ── Curva de evolución de ejecuciones ───────────────────────────────────────
interface PuntoEvo {
  id: number;
  fecha: string;
  cumpl: number | null;   // % de cumplimiento de esa ejecución
  hallazgos: number;      // cantidad de hallazgos de esa ejecución
}

function colorPct(v: number | null): string {
  if (v == null) return 'var(--text-muted)';
  return v >= 90 ? 'var(--ok-fg)' : v >= 70 ? 'var(--warn-fg)' : 'var(--err-fg)';
}

function fechaCorta(iso: string): string {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('es-AR', { day: '2-digit', month: '2-digit' });
}

function CurvaEvolucion({ pts, selId, onSelect }: {
  pts: PuntoEvo[]; selId: number | null; onSelect: (id: number) => void;
}) {
  if (pts.length === 0) {
    return <div className="pd-empty">Todavía no hay ejecuciones completadas para graficar la evolución.</div>;
  }
  // viewBox en escala ~px reales (igual criterio que el gráfico del dashboard):
  // así la tipografía y los elementos quedan al mismo tamaño natural.
  const W = 1480, H = 330, padL = 44, padR = 28, padT = 40, padB = 66;
  const innerW = W - padL - padR, innerH = H - padT - padB;
  const n = pts.length;
  const X = (i: number) => (n > 1 ? padL + i * (innerW / (n - 1)) : padL + innerW / 2);
  const Y = (v: number) => padT + (1 - v / 100) * innerH;

  const vp = pts.map((p, i) => ({ ...p, i, vx: X(i), vy: Y(p.cumpl ?? 0) }));
  const line = vp.map((p, k) => `${k ? 'L' : 'M'}${p.vx.toFixed(1)} ${p.vy.toFixed(1)}`).join(' ');
  const baseY = padT + innerH;
  const area = `${line} L ${vp[n - 1].vx.toFixed(1)} ${baseY.toFixed(1)} L ${vp[0].vx.toFixed(1)} ${baseY.toFixed(1)} Z`;

  return (
    <div>
      <svg width="100%" viewBox={`0 0 ${W} ${H}`} className="pd-evo-svg">
        <defs>
          <linearGradient id="evoGrad" x1="0" y1="0" x2="0" y2="1">
            <stop offset="0%" stopColor="var(--primary)" stopOpacity="0.20" />
            <stop offset="100%" stopColor="var(--primary)" stopOpacity="0" />
          </linearGradient>
        </defs>

        {/* Guías 0 / 50 / 100 + eje Y */}
        {[0, 50, 100].map((g) => (
          <g key={g}>
            <line x1={padL} y1={Y(g)} x2={W - padR} y2={Y(g)} className="pd-evo-grid" />
            <text x={padL - 10} y={Y(g) + 4} className="pd-evo-axis" textAnchor="end">{g}</text>
          </g>
        ))}
        {/* Umbrales de cumplimiento 70 / 90 */}
        {[70, 90].map((g) => (
          <line key={g} x1={padL} y1={Y(g)} x2={W - padR} y2={Y(g)} className="pd-evo-thr" />
        ))}

        {n > 1 && <path d={area} fill="url(#evoGrad)" />}
        {n > 1 && <path d={line} className="pd-evo-line" fill="none" />}

        {vp.map((p) => (
          <g key={p.id} className="pd-evo-pt" onClick={() => onSelect(p.id)}>
            <circle cx={p.vx} cy={p.vy} r={p.id === selId ? 8 : 6}
              fill="var(--surface)" stroke={colorPct(p.cumpl)} strokeWidth={p.id === selId ? 3 : 2.5} />
            <text x={p.vx} y={p.vy - 14} className="pd-evo-val" textAnchor="middle" fill={colorPct(p.cumpl)}>
              {p.cumpl == null ? '—' : `${p.cumpl}%`}
            </text>
            <text x={p.vx} y={baseY + 24} className="pd-evo-x" textAnchor="middle">{fechaCorta(p.fecha)}</text>
            <text x={p.vx} y={baseY + 42} className="pd-evo-x2" textAnchor="middle">{p.hallazgos} hz</text>
          </g>
        ))}
      </svg>
      <div className="pd-evo-foot">
        {n === 1
          ? 'Hay una sola ejecución completada. La curva se va a poblar a medida que vuelvas a ejecutar la auditoría sobre este proyecto.'
          : 'Cada punto es una ejecución de la auditoría sobre este proyecto. Tocá un punto para ver el detalle de esa ejecución.'}
      </div>
    </div>
  );
}

export default function ProyectoDetalle() {
  useInjectStyle(proyectoDetalleCss, 'proyecto-detalle-style');
  const { id } = useParams<{ id: string }>();
  const proyectoId = Number(id);
  const navigate = useNavigate();

  const [proyecto, setProyecto] = useState<Proyecto | null>(null);
  const [auditorias, setAuditorias] = useState<AuditoriaDeProyecto[]>([]);
  const [etapas, setEtapas] = useState<Etapa[]>([]);
  const [procNombre, setProcNombre] = useState<string>('');
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');

  const [selId, setSelId] = useState<number | null>(null);
  const [resultado, setResultado] = useState<AuditoriaResultado | null>(null);
  const [errores, setErrores] = useState<RegistroError[]>([]);
  const [informes, setInformes] = useState<Informe[]>([]);
  const [cargandoSel, setCargandoSel] = useState(false);
  const [expandido, setExpandido] = useState<Set<number>>(new Set());
  const [verInforme, setVerInforme] = useState<Informe | null>(null);
  const [pdfUrl, setPdfUrl] = useState<string | null>(null);
  const [pdfError, setPdfError] = useState(false);
  const [descargando, setDescargando] = useState<number | null>(null);

  // Curva de evolución: una métrica por cada ejecución (auditoría) completada.
  const [evolucion, setEvolucion] = useState<PuntoEvo[]>([]);

  // Carga inicial: proyecto + auditorías + etapas + nombre de procedimiento.
  useEffect(() => {
    let activo = true;
    (async () => {
      try {
        setCargando(true); setError('');
        const proy = await obtenerProyecto(proyectoId);
        const [auds, procs, eta] = await Promise.all([
          listarAuditoriasDeProyecto(proyectoId),
          listarProcedimientos(),
          listarEtapasDeProcedimiento(proy.procedimientoId).catch(() => [] as Etapa[]),
        ]);
        if (!activo) return;
        setProyecto(proy);
        setEtapas(eta);
        const p = procs.find((x) => x.id === proy.procedimientoId);
        setProcNombre(p ? `${p.codigo} · ${p.nombre}` : `Procedimiento #${proy.procedimientoId}`);
        // ordenar auditorías por fecha desc y seleccionar la primera
        auds.sort((a, b) => +new Date(b.fechaInicioUtc) - +new Date(a.fechaInicioUtc));
        setAuditorias(auds);
        setSelId(auds[0]?.id ?? null);
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'No se pudo cargar el proyecto.';
        if (activo) setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
      } finally {
        if (activo) setCargando(false);
      }
    })();
    return () => { activo = false; };
  }, [proyectoId]);

  // Al seleccionar una auditoría: cargar resultado + informes. Si la auditoría
  // falló, además traemos su log de errores para mostrar el motivo.
  useEffect(() => {
    if (selId == null) { setResultado(null); setInformes([]); setErrores([]); return; }
    const fallo = auditorias.find((a) => a.id === selId)?.estado === 'Fallida';
    let activo = true;
    (async () => {
      try {
        setCargandoSel(true);
        setExpandido(new Set());
        const [res, infs, errs] = await Promise.all([
          obtenerResultado(selId).catch(() => null),
          listarInformesDeAuditoria(selId).catch(() => [] as Informe[]),
          fallo ? obtenerErrores(selId).catch(() => [] as RegistroError[]) : Promise.resolve([] as RegistroError[]),
        ]);
        if (!activo) return;
        setResultado(res);
        setInformes(infs);
        setErrores(errs);
      } finally {
        if (activo) setCargandoSel(false);
      }
    })();
    return () => { activo = false; };
  }, [selId, auditorias]);

  // Curva de evolución: para cada ejecución completada (orden cronológico),
  // computa el % de cumplimiento y la cantidad de hallazgos de ese resultado.
  useEffect(() => {
    const completadas = auditorias
      .filter((a) => a.estado === 'Completada')
      .sort((a, b) => +new Date(a.fechaInicioUtc) - +new Date(b.fechaInicioUtc));
    if (completadas.length === 0) { setEvolucion([]); return; }
    let activo = true;
    (async () => {
      const resultados = await Promise.all(
        completadas.map((a) => obtenerResultado(a.id).catch(() => null)),
      );
      if (!activo) return;
      setEvolucion(completadas.map((a, i) => {
        const c = calcular(resultados[i]);
        return {
          id: a.id,
          fecha: a.fechaInicioUtc,
          cumpl: c ? c.pct : null,
          hallazgos: resultados[i]?.contadores.hallazgos ?? 0,
        };
      }));
    })();
    return () => { activo = false; };
  }, [auditorias]);

  // Vista previa del informe: genera el PDF y lo embebe como blob en el modal.
  useEffect(() => {
    if (!verInforme) { setPdfUrl(null); setPdfError(false); return; }
    let activo = true;
    let url: string | null = null;
    setPdfUrl(null); setPdfError(false);
    previsualizarInformePdf(verInforme)
      .then((u) => { if (activo) { url = u; setPdfUrl(u); } else URL.revokeObjectURL(u); })
      .catch(() => { if (activo) setPdfError(true); });
    return () => { activo = false; if (url) URL.revokeObjectURL(url); };
  }, [verInforme]);

  const handleDescargar = async (inf: Informe) => {
    setDescargando(inf.id);
    try {
      await descargarInformePdf(inf);
    } catch {
      alert('No se pudo generar el PDF del informe.');
    } finally {
      setDescargando((d) => (d === inf.id ? null : d));
    }
  };

  const etapaNombre = useMemo(() => {
    const m = new Map<number, string>();
    etapas.forEach((e) => m.set(e.id, e.nombre));
    return (idEtapa: number) => m.get(idEtapa) ?? `Etapa #${idEtapa}`;
  }, [etapas]);

  const cumpl = calcular(resultado);
  const audSel = auditorias.find((a) => a.id === selId) ?? null;
  const fallo = audSel?.estado === 'Fallida';
  const operadores = proyecto?.responsables.filter((r) => r.rol === 'Operador') ?? [];

  const toggle = (artId: number) =>
    setExpandido((prev) => {
      const n = new Set(prev);
      n.has(artId) ? n.delete(artId) : n.add(artId);
      return n;
    });

  if (cargando) return <p className="loading-text">Cargando proyecto…</p>;
  if (error) return (
    <>
      <button className="pd-back" onClick={() => navigate('/proyectos')}>← Volver a Proyectos</button>
      <div className="error-banner">{error}</div>
    </>
  );
  if (!proyecto) return null;

  return (
    <>
      <button className="pd-back" onClick={() => navigate('/proyectos')}>
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m15 18-6-6 6-6" /></svg>
        Volver a Proyectos
      </button>

      {/* ── Cabecera ── */}
      <div className="pd-head">
        <div className="pd-head-top">
          <div>
            <div className="pd-title">{proyecto.nombre}</div>
            {proyecto.descripcion && <div className="pd-desc">{proyecto.descripcion}</div>}
          </div>
          <div className="pd-badges">
            <span className="badge b-info">Tipo {proyecto.tipoProyecto}</span>
            <span className={`badge ${proyecto.activo ? 'b-ok' : 'b-err'}`}>{proyecto.activo ? 'Activo' : 'Inactivo'}</span>
          </div>
        </div>

        <div className="pd-grid">
          <div className="pd-field">
            <div className="k">Procedimiento</div>
            <div className="v">{procNombre}</div>
          </div>
          <div className="pd-field">
            <div className="k">Fechas</div>
            <div className="v">{fmt(proyecto.fechaInicio)} → {fmt(proyecto.fechaFin)}</div>
          </div>
          <div className="pd-field">
            <div className="k">Horas estimadas</div>
            <div className="v">{proyecto.horasEstimadas ? `${proyecto.horasEstimadas} h` : '—'}</div>
          </div>
          <div className="pd-field">
            <div className="k">Operadores</div>
            {operadores.length === 0 ? (
              <div className="v" style={{ color: 'var(--text-muted)' }}>Sin asignar</div>
            ) : (
              <div className="pd-resp">
                {operadores.map((o) => (
                  <span key={o.id} className="chip">
                    <span className="mini">{ini(o.nombre)}</span>
                    <span className="who">{o.nombre}</span>
                  </span>
                ))}
              </div>
            )}
          </div>
          <div className="pd-field" style={{ gridColumn: '1 / -1' }}>
            <div className="k">Integraciones</div>
            {(!proyecto.driveFolderId && !proyecto.trelloBoardId && !proyecto.clockifyProjectId) ? (
              <div className="v" style={{ color: 'var(--text-muted)' }}>Ninguna configurada</div>
            ) : (
              <div className="pd-chips">
                {proyecto.driveFolderId && <span className="pd-chip"><span className="dot" style={{ background: COLOR_INT.drive }} />Drive <code>{proyecto.driveFolderId}</code></span>}
                {proyecto.trelloBoardId && <span className="pd-chip"><span className="dot" style={{ background: COLOR_INT.trello }} />Trello <code>{proyecto.trelloBoardId}</code></span>}
                {proyecto.clockifyProjectId && <span className="pd-chip"><span className="dot" style={{ background: COLOR_INT.clockify }} />Clockify <code>{proyecto.clockifyProjectId}</code></span>}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* ── Evolución de ejecuciones ── */}
      <div className="pd-panel">
        <div className="pd-section-title">Evolución de ejecuciones</div>
        <CurvaEvolucion pts={evolucion} selId={selId} onSelect={setSelId} />
      </div>

      {/* ── Auditorías + detalle ── */}
      <div className="pd-cols">
        {/* Columna izquierda: lista de auditorías */}
        <div>
          <div className="pd-section-title">Auditorías ({auditorias.length})</div>
          {auditorias.length === 0 ? (
            <div className="pd-empty">Este proyecto todavía no tiene auditorías.</div>
          ) : (
            auditorias.map((a) => (
              <button
                key={a.id}
                className={`pd-aud${a.id === selId ? ' sel' : ''}`}
                onClick={() => setSelId(a.id)}
              >
                <div className="row1">
                  <span className="etapa">{etapaNombre(a.etapaId)}</span>
                  <span className={`badge ${ESTADO_AUD[a.estado] ?? 'b-gray'}`}>{a.estado}</span>
                </div>
                <div className="sub">{fmt(a.fechaInicioUtc)} · {a.nombreAuditor}</div>
              </button>
            ))
          )}
        </div>

        {/* Columna derecha: resultado de la auditoría seleccionada */}
        <div>
          {selId == null ? (
            <div className="pd-empty">Seleccioná una auditoría para ver su resultado.</div>
          ) : cargandoSel ? (
            <p className="loading-text">Cargando resultado…</p>
          ) : fallo ? (
            <div className="pd-panel">
              <div className="pd-fail-head">
                <span className="pd-fail-badge" aria-hidden>
                  <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
                    <path d="M10.3 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.7 3.86a2 2 0 0 0-3.4 0Z" strokeLinejoin="round" />
                    <path d="M12 9v4M12 17h.01" strokeLinecap="round" />
                  </svg>
                </span>
                <div>
                  <div className="pd-fail-title">La auditoría falló</div>
                  <div className="pd-fail-sub">
                    {audSel?.mensajeError ??
                      'Ocurrió un error durante la ejecución. Revisá la configuración del proyecto e intentá de nuevo.'}
                  </div>
                </div>
              </div>

              {errores.length > 0 && (
                <ul className="pd-fail-list">
                  {errores.map((e) => (
                    <li key={e.id} className="pd-fail-item">
                      <span className="pd-fail-nodo">{labelNodo(e.nodo)}</span>
                      <span className="pd-fail-msg">{e.mensaje}</span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          ) : (
            <>
              <div className="pd-panel">
                <div className="pd-stats">
                  <div className="pd-stat"><div className="n">{resultado?.contadores.artefactosEvaluados ?? 0}</div><div className="l">Artefactos evaluados</div></div>
                  <div className="pd-stat"><div className="n">{resultado?.contadores.hallazgos ?? 0}</div><div className="l">Hallazgos</div></div>
                  <div className="pd-stat"><div className="n">{resultado?.contadores.documentosAnalizados ?? 0}</div><div className="l">Documentos analizados</div></div>
                </div>

                <div className="pd-ringbox">
                  {cumpl ? (
                    <>
                      <div className="pd-ring">
                        <svg width="64" height="64" viewBox="0 0 64 64">
                          <circle className="track" cx="32" cy="32" r={R} fill="none" strokeWidth="6" />
                          <circle
                            className={`bar ${cumpl.pct >= 90 ? 'ok' : cumpl.pct >= 70 ? 'warn' : 'err'}`}
                            cx="32" cy="32" r={R} fill="none" strokeWidth="6"
                            strokeDasharray={C} strokeDashoffset={C * (1 - cumpl.pct / 100)}
                          />
                        </svg>
                        <span className="center">{cumpl.pct}%</span>
                      </div>
                      <div>
                        <div className="pd-ring-t">Cumplimiento</div>
                        <div className="pd-ring-s">{cumpl.conf}/{cumpl.ev} artefactos conformes</div>
                      </div>
                    </>
                  ) : (
                    <>
                      <div className="pd-ring empty">
                        <svg width="64" height="64" viewBox="0 0 64 64"><circle className="track" cx="32" cy="32" r={R} fill="none" strokeWidth="6" /></svg>
                        <span className="center">—</span>
                      </div>
                      <div>
                        <div className="pd-ring-t">Cumplimiento</div>
                        <div className="pd-ring-s">Aún sin artefactos evaluados</div>
                      </div>
                    </>
                  )}
                </div>

                <div className="pd-section-title" style={{ fontSize: '14px' }}>Artefactos evaluados</div>
                {(!resultado || resultado.artefactosEvaluados.length === 0) ? (
                  <div className="pd-empty">Sin artefactos evaluados en esta auditoría.</div>
                ) : (
                  resultado.artefactosEvaluados.map((art) => {
                    const abierto = expandido.has(art.id);
                    return (
                      <div key={art.id} className={`pd-art${abierto ? ' open' : ''}`}>
                        <div className="pd-art-head" onClick={() => toggle(art.id)}>
                          <svg className="pd-caret" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m9 6 6 6-6 6" /></svg>
                          <span className="pd-art-name">
                            {art.artefactoEsperadoCodigo && <code>{art.artefactoEsperadoCodigo}</code>}
                            {art.artefactoEsperadoNombre ?? `Artefacto #${art.artefactoEsperadoId}`}
                          </span>
                          <span className={`badge ${art.aplica === 'Aplica' ? 'b-info' : 'b-gray'}`}>{APLICA_LABEL[art.aplica] ?? art.aplica}</span>
                          <span className={`badge ${RESULTADO_BADGE[art.resultado] ?? 'b-gray'}`}>{RESULTADO_LABEL[art.resultado] ?? art.resultado}</span>
                        </div>
                        <div className="pd-art-body">
                          {art.observaciones && <div className="pd-art-obs">{art.observaciones}</div>}

                          {art.hallazgos.length > 0 && (
                            <>
                              <div className="pd-sub-t">Hallazgos ({art.hallazgos.length})</div>
                              {art.hallazgos.map((h) => (
                                <div key={h.id} className="pd-hz">
                                  <div className="top">
                                    <span className={`badge ${h.tipo === 'NoConformidad' ? 'b-err' : h.tipo === 'Observacion' ? 'b-warn' : 'b-info'}`}>{h.tipo}</span>
                                    <span className="ag">{h.agenteOrigen}</span>
                                  </div>
                                  <div className="desc">{h.descripcion}</div>
                                  {h.justificacion && <div className="just">{h.justificacion}</div>}
                                </div>
                              ))}
                            </>
                          )}

                          {art.documentosAnalizados.length > 0 && (
                            <>
                              <div className="pd-sub-t">Documentos analizados ({art.documentosAnalizados.length})</div>
                              {art.documentosAnalizados.map((d) => (
                                <div key={d.id} className="pd-doc">
                                  <svg className="fa" viewBox="0 0 24 24" width="14" height="14" fill="none" stroke="currentColor" strokeWidth="1.8"><path d="M14 3v5h5" /><path d="M6 3h8l5 5v11a2 2 0 0 1-2 2H6a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2z" /></svg>
                                  <span>{d.nombreArchivo}</span>
                                  <span className="hash">· {d.fuente} · {d.hashContenido.slice(0, 10)}…</span>
                                </div>
                              ))}
                            </>
                          )}

                          {art.hallazgos.length === 0 && art.documentosAnalizados.length === 0 && !art.observaciones && (
                            <div className="pd-art-obs">Sin hallazgos ni documentos asociados.</div>
                          )}
                        </div>
                      </div>
                    );
                  })
                )}
              </div>

              {/* Informes de la auditoría seleccionada */}
              <div className="pd-panel">
                <div className="pd-section-title" style={{ fontSize: '14px' }}>Informes</div>
                {informes.length === 0 ? (
                  <div className="pd-empty">Esta auditoría todavía no tiene informes generados.</div>
                ) : (
                  informes.map((inf) => (
                    <div key={inf.id} className="pd-inf">
                      <div className="meta">
                        <b>{TIPO_INFORME_LABEL[inf.tipo] ?? inf.tipo}</b> · {fmt(inf.fechaGeneracion)}
                      </div>
                      <div className="acts">
                        <button className="pd-link" onClick={() => setVerInforme(inf)}>
                          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" /></svg>Ver
                        </button>
                        <button className="pd-link" onClick={() => handleDescargar(inf)} disabled={descargando === inf.id}>
                          <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8"><path d="M12 3v12M7 10l5 5 5-5M5 21h14" /></svg>{descargando === inf.id ? 'Generando…' : 'PDF'}
                        </button>
                      </div>
                    </div>
                  ))
                )}
              </div>
            </>
          )}
        </div>
      </div>

      {/* Modal ver informe */}
      {verInforme && (
        <div className="pd-overlay" onClick={() => setVerInforme(null)}>
          <div className="pd-modal" onClick={(e) => e.stopPropagation()}>
            <div className="pd-modal-head">
              <div>
                <div className="pd-modal-title">{verInforme.nombreProyecto}</div>
                <div className="pd-modal-sub">{TIPO_INFORME_LABEL[verInforme.tipo] ?? verInforme.tipo} · {fmt(verInforme.fechaGeneracion)}</div>
              </div>
              <button className="pd-modal-close" onClick={() => setVerInforme(null)} aria-label="Cerrar">×</button>
            </div>
            <div className="pd-modal-body">
              {pdfError ? (
                <div className="pd-empty">No se pudo generar la vista previa del PDF.</div>
              ) : pdfUrl ? (
                <iframe className="pd-pdf-frame" src={pdfUrl} title="Vista previa del informe" />
              ) : (
                <div className="pd-empty">Generando vista previa…</div>
              )}
            </div>
            <div className="pd-modal-foot">
              <button className="btn-pri" onClick={() => handleDescargar(verInforme)} disabled={descargando === verInforme.id}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" width="15" height="15"><path d="M12 3v12M7 10l5 5 5-5M5 21h14" /></svg>{descargando === verInforme.id ? 'Generando…' : 'Descargar PDF'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}