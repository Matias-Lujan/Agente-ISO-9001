// src/screens/Proyectos.tsx
// ============================================================================
//  Pantalla de Proyectos.
//
//  Visibilidad por rol (la lista la filtra el backend según el JWT):
//   - Auditor / Administrador → ven TODOS los proyectos
//   - Operador                → ve solo los proyectos que tiene asignados
//
//  El botón "+ Nuevo proyecto" y su modal SOLO los ve el Administrador.
//  (Además el backend valida POST /api/proyectos con [Authorize(Roles=Admin)].)
//
//  El tipo de proyecto es A o B (no hay C): el tipo determina qué artefactos del
//  procedimiento son obligatorios (MandatorioTipoA / MandatorioTipoB), por eso
//  el tailoring no se carga a mano acá — se deriva de procedimiento + tipo.
// ============================================================================
import { useEffect, useMemo, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useInjectStyle } from '../utils/useInjectStyle';
import { proyectosCss } from '../styles/proyectos';
import { useAuth } from '../login/AuthContext';
import {
  listarProyectos,
  crearProyecto,
  asignarResponsable,
  type Proyecto,
  type CrearProyectoRequest,
} from '../api/proyectos';
import { listarProcedimientos, type Procedimiento } from '../api/procedimientos';
import {
  listarAuditoriasDeProyecto,
  obtenerResultado,
  type AuditoriaResultado,
} from '../api/auditorias';
import { listarUsuarios, type UsuarioResponse } from '../login/authApi';

type Filtro = 'Todos' | 'Activos' | 'Inactivos';

const FORM_VACIO = {
  nombre: '',
  descripcion: '',
  tipoProyecto: 'A',
  procedimientoId: '',
  fechaInicio: '',
  fechaFin: '',
  horasEstimadas: '',
  driveFolderId: '',
  trelloBoardId: '',
  clockifyProjectId: '',
};

// Iniciales para el avatar (hasta 2 letras).
function iniciales(nombre: string): string {
  return nombre.trim().split(/\s+/).map((p) => p[0]).slice(0, 2).join('').toUpperCase();
}

function fmtFecha(iso: string | null): string | null {
  if (!iso) return null;
  const d = new Date(iso);
  return isNaN(d.getTime()) ? null : d.toLocaleDateString('es-AR');
}

// ── Cumplimiento (anillo) ──────────────────────────────────────────────────
type Cumplimiento =
  | { estado: 'cargando' }
  | { estado: 'sindatos' }
  | { estado: 'ok'; pct: number; conformes: number; evaluados: number; pendientes: number };

// % = artefactos conformes / artefactos aplicables ya evaluados (con veredicto).
function calcularCumplimiento(res: AuditoriaResultado): Cumplimiento {
  const aplicables  = res.artefactosEvaluados.filter((a) => a.aplica === 'Aplica');
  const conformes   = aplicables.filter((a) => a.resultado === 'Conforme').length;
  const noConformes = aplicables.filter((a) => a.resultado === 'NoConforme').length;
  const pendientes  = aplicables.filter((a) => a.resultado === 'PendienteEtapaFutura').length;
  const evaluados   = conformes + noConformes;
  if (evaluados === 0) return { estado: 'sindatos' };
  return {
    estado: 'ok',
    pct: Math.round((conformes / evaluados) * 100),
    conformes, evaluados, pendientes,
  };
}

const RING_R = 24;
const RING_C = 2 * Math.PI * RING_R;

function Anillo({ c }: { c: Cumplimiento | undefined }) {
  if (!c || c.estado === 'cargando') {
    return (
      <div className="pr-ring loading">
        <svg width="58" height="58" viewBox="0 0 58 58">
          <circle className="track" cx="29" cy="29" r={RING_R} fill="none" strokeWidth="6" />
        </svg>
        <span className="center">…</span>
      </div>
    );
  }
  if (c.estado === 'sindatos') {
    return (
      <div className="pr-ring empty">
        <svg width="58" height="58" viewBox="0 0 58 58">
          <circle className="track" cx="29" cy="29" r={RING_R} fill="none" strokeWidth="6" />
        </svg>
        <span className="center">—</span>
      </div>
    );
  }
  const cls = c.pct >= 90 ? 'ok' : c.pct >= 70 ? 'warn' : 'err';
  return (
    <div className="pr-ring">
      <svg width="58" height="58" viewBox="0 0 58 58">
        <circle className="track" cx="29" cy="29" r={RING_R} fill="none" strokeWidth="6" />
        <circle
          className={`bar ${cls}`} cx="29" cy="29" r={RING_R} fill="none" strokeWidth="6"
          strokeDasharray={RING_C} strokeDashoffset={RING_C * (1 - c.pct / 100)}
        />
      </svg>
      <span className="center">{c.pct}%</span>
    </div>
  );
}

const COLOR_INT: Record<string, string> = {
  drive: '#1FA463',
  trello: '#0079BF',
  clockify: '#E5704B',
};

export default function Proyectos() {
  useInjectStyle(proyectosCss, 'proyectos-style');

  const { usuario } = useAuth();
  const esAdmin = usuario?.rol === 'Administrador';
  const navigate = useNavigate();

  const [proyectos, setProyectos]       = useState<Proyecto[]>([]);
  const [procedimientos, setProcs]      = useState<Procedimiento[]>([]);
  const [operadores, setOperadores]     = useState<UsuarioResponse[]>([]);
  const [cargando, setCargando]         = useState(true);
  const [error, setError]               = useState('');
  const [filtro, setFiltro]             = useState<Filtro>('Todos');
  const [cumpl, setCumpl]               = useState<Record<number, Cumplimiento>>({});

  // Modal de creación (solo admin)
  const [modalAbierto, setModalAbierto] = useState(false);
  const [form, setForm]                 = useState(FORM_VACIO);
  const [opsSel, setOpsSel]             = useState<number[]>([]);
  const [guardando, setGuardando]       = useState(false);
  const [errorModal, setErrorModal]     = useState('');

  // ── Carga inicial ─────────────────────────────────────────────────────────
  const recargar = async () => {
    try {
      setCargando(true);
      setError('');
      const [proys, procs] = await Promise.all([listarProyectos(), listarProcedimientos()]);
      setProyectos(proys);
      setProcs(procs);
      // Operadores solo los necesita el admin (para asignar en el modal).
      if (esAdmin) {
        const usuarios = await listarUsuarios();
        setOperadores(usuarios.filter((u) => u.rol === 'Operador' && u.activo));
      }
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'Error al cargar proyectos.';
      setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setCargando(false);
    }
  };

  useEffect(() => {
    recargar();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [esAdmin]);

  // Cumplimiento por proyecto: última auditoría completada → resultado → %.
  // Se calcula aparte para que las tarjetas aparezcan ya y el anillo se llene
  // de a uno (datos reales, sin bloquear el render).
  useEffect(() => {
    if (proyectos.length === 0) { setCumpl({}); return; }
    let activo = true;
    setCumpl(Object.fromEntries(
      proyectos.map((p) => [p.id, { estado: 'cargando' } as Cumplimiento]),
    ));
    (async () => {
      for (const p of proyectos) {
        let res: Cumplimiento = { estado: 'sindatos' };
        try {
          const auds = await listarAuditoriasDeProyecto(p.id);
          const completadas = auds.filter((a) => a.estado === 'Completada');
          if (completadas.length > 0) {
            completadas.sort((a, b) => +new Date(b.fechaInicioUtc) - +new Date(a.fechaInicioUtc));
            res = calcularCumplimiento(await obtenerResultado(completadas[0].id));
          }
        } catch {
          res = { estado: 'sindatos' };
        }
        if (!activo) return;
        setCumpl((prev) => ({ ...prev, [p.id]: res }));
      }
    })();
    return () => { activo = false; };
  }, [proyectos]);

  // Mapa id → procedimiento para mostrar el código/nombre en cada tarjeta.
  const procPorId = useMemo(() => {
    const m = new Map<number, Procedimiento>();
    procedimientos.forEach((p) => m.set(p.id, p));
    return m;
  }, [procedimientos]);

  const visibles = useMemo(() => {
    if (filtro === 'Activos')   return proyectos.filter((p) => p.activo);
    if (filtro === 'Inactivos') return proyectos.filter((p) => !p.activo);
    return proyectos;
  }, [proyectos, filtro]);

  // ── Modal ───────────────────────────────────────────────────────────────
  const abrirModal = () => {
    setForm({ ...FORM_VACIO, procedimientoId: String(procedimientos[0]?.id ?? '') });
    setOpsSel([]);
    setErrorModal('');
    setModalAbierto(true);
  };
  const cerrarModal = () => setModalAbierto(false);

  const toggleOp = (id: number) =>
    setOpsSel((prev) => (prev.includes(id) ? prev.filter((x) => x !== id) : [...prev, id]));

  const set = (campo: keyof typeof FORM_VACIO, valor: string) =>
    setForm((f) => ({ ...f, [campo]: valor }));

  const guardar = async () => {
    if (!form.nombre.trim()) { setErrorModal('El nombre es obligatorio.'); return; }
    if (!form.procedimientoId) { setErrorModal('Elegí un procedimiento.'); return; }

    const req: CrearProyectoRequest = {
      nombre: form.nombre.trim(),
      descripcion: form.descripcion.trim() || null,
      tipoProyecto: form.tipoProyecto,
      procedimientoId: Number(form.procedimientoId),
      fechaInicio: form.fechaInicio || null,
      fechaFin: form.fechaFin || null,
      horasEstimadas: form.horasEstimadas ? Number(form.horasEstimadas) : null,
      driveFolderId: form.driveFolderId.trim() || null,
      trelloBoardId: form.trelloBoardId.trim() || null,
      clockifyProjectId: form.clockifyProjectId.trim() || null,
    };

    try {
      setGuardando(true);
      setErrorModal('');
      const creado = await crearProyecto(req);
      // Asignamos los operadores elegidos (endpoint aparte, uno por uno).
      await Promise.all(opsSel.map((uid) => asignarResponsable(creado.id, uid)));
      await recargar();
      setModalAbierto(false);
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'No se pudo crear el proyecto.';
      setErrorModal(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setGuardando(false);
    }
  };

  // ── Render de operadores de una tarjeta ───────────────────────────────────
  const renderAsignados = (p: Proyecto) => {
    const ops = p.responsables.filter((r) => r.rol === 'Operador');
    if (ops.length === 0) {
      return (
        <div className="pr-assignee none">
          <div className="pr-ph">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
              <circle cx="12" cy="8" r="3.5" /><path d="M5 20a7 7 0 0 1 14 0" />
            </svg>
          </div>
          <span>Sin operador asignado</span>
        </div>
      );
    }
    if (ops.length === 1) {
      return (
        <div className="pr-assignee">
          <div className="pr-mini">{iniciales(ops[0].nombre)}</div>
          <div><span className="pr-who">{ops[0].nombre}</span> <span className="pr-role">· Operador</span></div>
        </div>
      );
    }
    const visiblesOps = ops.slice(0, 2);
    return (
      <div className="pr-assignee">
        <div className="pr-stack">
          {visiblesOps.map((o) => <div key={o.id} className="pr-mini">{iniciales(o.nombre)}</div>)}
          {ops.length > 2 && <div className="pr-mini">+{ops.length - 2}</div>}
        </div>
        <div>
          <span className="pr-who">{visiblesOps.map((o) => o.nombre).join(', ')}</span>{' '}
          <span className="pr-role">{ops.length > 2 ? `y ${ops.length - 2} más` : '· Operadores'}</span>
        </div>
      </div>
    );
  };

  const renderChips = (p: Proyecto) => {
    const chips: { k: string; label: string }[] = [];
    if (p.driveFolderId)     chips.push({ k: 'drive', label: 'Drive' });
    if (p.trelloBoardId)     chips.push({ k: 'trello', label: 'Trello' });
    if (p.clockifyProjectId) chips.push({ k: 'clockify', label: 'Clockify' });
    if (chips.length === 0) return null;
    return (
      <div className="pr-chips">
        {chips.map((c) => (
          <span key={c.k} className="pr-chip">
            <span className="dot" style={{ background: COLOR_INT[c.k] }} />{c.label}
          </span>
        ))}
      </div>
    );
  };

  return (
    <>
      <div className="topbar">
        <div>
          <h1 className="page-title">Proyectos</h1>
          <p className="page-sub">
            {usuario?.rol === 'Operador'
              ? 'Proyectos que tenés asignados'
              : 'Auditorías ISO 9001 por proyecto'}
          </p>
        </div>
        {/* El botón solo lo ve el Administrador */}
        {esAdmin && (
          <button className="btn-pri" onClick={abrirModal}>
            <svg viewBox="0 0 16 16" fill="none" width="15" height="15">
              <path d="M8 3v10M3 8h10" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" />
            </svg>
            Nuevo proyecto
          </button>
        )}
      </div>

      <div className="pr-filters">
        {(['Todos', 'Activos', 'Inactivos'] as Filtro[]).map((f) => (
          <button
            key={f}
            className={`pr-pill${filtro === f ? ' active' : ''}`}
            onClick={() => setFiltro(f)}
          >
            {f}
          </button>
        ))}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {cargando ? (
        <p className="loading-text">Cargando proyectos…</p>
      ) : visibles.length === 0 ? (
        <div className="pr-empty">
          {usuario?.rol === 'Operador'
            ? 'Todavía no tenés proyectos asignados.'
            : 'No hay proyectos para mostrar.'}
        </div>
      ) : (
        <div className="pr-grid">
          {visibles.map((p) => {
            const proc = procPorId.get(p.procedimientoId);
            return (
              <div key={p.id} className="pr-card" onClick={() => navigate(`/proyectos/${p.id}`)}>
                <div className="pr-top">
                  <div className="pr-name">{p.nombre}</div>
                  <span className="pr-badge tipo">Tipo {p.tipoProyecto}</span>
                </div>
                <div className="pr-meta">
                  {fmtFecha(p.fechaInicio) ? `Inicio · ${fmtFecha(p.fechaInicio)}` : 'Sin fecha de inicio'}
                </div>

                {renderAsignados(p)}
                {renderChips(p)}

                <div className="pr-proc">
                  <span className="tag">{proc ? `${proc.codigo} · ${proc.nombre}` : `Procedimiento #${p.procedimientoId}`}</span>
                  <span className="sub">Tipo {p.tipoProyecto} · el tailoring se deriva del procedimiento</span>
                </div>

                <div className="pr-compliance">
                  <Anillo c={cumpl[p.id]} />
                  <div className="pr-comp-info">
                    <div className="pr-comp-title">Cumplimiento</div>
                    {(() => {
                      const c = cumpl[p.id];
                      if (!c || c.estado === 'cargando') return <div className="pr-comp-sub">Calculando…</div>;
                      if (c.estado === 'sindatos')        return <div className="pr-comp-sub">Sin auditoría completada</div>;
                      return (
                        <>
                          <div className="pr-comp-sub">{c.conformes}/{c.evaluados} artefactos conformes</div>
                          {c.pendientes > 0 && (
                            <div className="pr-comp-pend">{c.pendientes} pendiente{c.pendientes > 1 ? 's' : ''} de etapas futuras</div>
                          )}
                        </>
                      );
                    })()}
                  </div>
                </div>

                <div className="pr-foot">
                  <span className={`pr-badge ${p.activo ? 'ok' : 'off'}`}>
                    {p.activo ? 'Activo' : 'Inactivo'}
                  </span>
                  <span className="pr-hours">
                    {p.horasEstimadas ? <><b>{p.horasEstimadas}</b> h estimadas</> : 'Sin estimación'}
                  </span>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* ── Modal Nuevo proyecto (solo Administrador) ── */}
      {esAdmin && modalAbierto && (
        <div className="pr-overlay" onClick={cerrarModal}>
          <div className="pr-modal" onClick={(e) => e.stopPropagation()}>
            <div className="pr-modal-head">
              <div>
                <div className="pr-modal-title">Nuevo proyecto</div>
                <div className="pr-modal-sub">Completá los datos para registrar un proyecto a auditar</div>
              </div>
              <button className="pr-modal-close" onClick={cerrarModal} aria-label="Cerrar">×</button>
            </div>

            <div className="pr-modal-body">
              {errorModal && <div className="error-banner">{errorModal}</div>}

              <div className="pr-fld">
                <label>Nombre del proyecto *</label>
                <input
                  value={form.nombre}
                  onChange={(e) => set('nombre', e.target.value)}
                  placeholder="Ej. Migración FTTH — Zona Sur"
                />
              </div>

              <div className="pr-fld">
                <label>Descripción</label>
                <textarea
                  value={form.descripcion}
                  onChange={(e) => set('descripcion', e.target.value)}
                  placeholder="Breve descripción del alcance…"
                />
              </div>

              <div className="pr-row2">
                <div className="pr-fld">
                  <label>Tipo de proyecto</label>
                  <select value={form.tipoProyecto} onChange={(e) => set('tipoProyecto', e.target.value)}>
                    <option value="A">Tipo A</option>
                    <option value="B">Tipo B</option>
                  </select>
                </div>
                <div className="pr-fld">
                  <label>Horas estimadas</label>
                  <input
                    type="number" min="0"
                    value={form.horasEstimadas}
                    onChange={(e) => set('horasEstimadas', e.target.value)}
                    placeholder="0"
                  />
                </div>
              </div>

              <div className="pr-fld">
                <label>Procedimiento</label>
                <select
                  value={form.procedimientoId}
                  onChange={(e) => set('procedimientoId', e.target.value)}
                >
                  {procedimientos.length === 0 && <option value="">Sin procedimientos cargados</option>}
                  {procedimientos.map((p) => (
                    <option key={p.id} value={p.id}>{p.codigo} — {p.nombre}</option>
                  ))}
                </select>
                <div className="hint">El agente valida contra este procedimiento interno de BDT (PR), no solo contra la norma ISO 9001.</div>
              </div>

              <div className="pr-row2">
                <div className="pr-fld">
                  <label>Fecha de inicio</label>
                  <input type="date" value={form.fechaInicio} onChange={(e) => set('fechaInicio', e.target.value)} />
                </div>
                <div className="pr-fld">
                  <label>Fecha de fin</label>
                  <input type="date" value={form.fechaFin} onChange={(e) => set('fechaFin', e.target.value)} />
                </div>
              </div>

              <div className="pr-fld">
                <label>ID de carpeta de Google Drive</label>
                <input value={form.driveFolderId} onChange={(e) => set('driveFolderId', e.target.value)} placeholder="Ej. 1A2b3C4d…" />
              </div>
              <div className="pr-fld">
                <label>ID del tablero de Trello</label>
                <input value={form.trelloBoardId} onChange={(e) => set('trelloBoardId', e.target.value)} placeholder="Ej. 5f8a3c2e…" />
              </div>
              <div className="pr-fld">
                <label>ID del proyecto de Clockify</label>
                <input value={form.clockifyProjectId} onChange={(e) => set('clockifyProjectId', e.target.value)} placeholder="Ej. 60c3a1f2…" />
              </div>

              <div className="pr-fld">
                <label>Operadores asignados</label>
                {operadores.length === 0 ? (
                  <div className="hint">No hay operadores activos para asignar.</div>
                ) : (
                  <div className="pr-opsel">
                    {operadores.map((o) => (
                      <button
                        key={o.id}
                        type="button"
                        className={`pr-opchip${opsSel.includes(o.id) ? ' sel' : ''}`}
                        onClick={() => toggleOp(o.id)}
                      >
                        {o.nombre}
                      </button>
                    ))}
                  </div>
                )}
              </div>
            </div>

            <div className="pr-modal-foot">
              <button className="btn-ghost" onClick={cerrarModal} disabled={guardando}>Cancelar</button>
              <button className="btn-pri" onClick={guardar} disabled={guardando}>
                {guardando ? 'Creando…' : 'Crear proyecto'}
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}