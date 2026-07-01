import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  obtenerAuditoria,
  obtenerErrores,
  obtenerProgreso,
  obtenerResultado,
  type AuditoriaResultado,
  type EstadoAuditoria,
  type EstadoNodo,
  type NodoWorkflow,
  type ProgresoNodo,
  type RegistroError,
} from '../api/auditorias';

interface Props {
  auditoriaId: number;
}

// Cumplimiento de la ejecución = conformes / (conformes + no conformes) entre
// los artefactos aplicables (misma regla que el resto de la app).
function calcCumpl(res: AuditoriaResultado | null) {
  if (!res) return null;
  const aplic = res.artefactosEvaluados.filter((a) => a.aplica === 'Aplica');
  const conf = aplic.filter((a) => a.resultado === 'Conforme').length;
  const nc = aplic.filter((a) => a.resultado === 'NoConforme').length;
  const ev = conf + nc;
  if (ev === 0) return null;
  return { pct: Math.round((conf / ev) * 100), conf, ev };
}

// Orden y etiquetas humanas de los 4 nodos LLM que se muestran como agentes.
// Coincide con el enum NodoWorkflow del backend (Models/NodoWorkflow.cs). El
// orden refleja la secuencia del workflow: DocumentAnalysis arranca; cuando
// termina, Compliance y Consistency corren en paralelo; cuando ambos terminan,
// FindingsClassification clasifica los hallazgos.
const AGENTES: { nodo: NodoWorkflow; icono: string; label: string }[] = [
  { nodo: 'DocumentAnalysis', icono: '📄', label: 'Análisis documental' },
  { nodo: 'ComplianceValidation', icono: '✅', label: 'Validación de procesos' },
  { nodo: 'ConsistencyVerification', icono: '🔍', label: 'Verificación de consistencia' },
  { nodo: 'FindingsClassification', icono: '🏷️', label: 'Clasificación de hallazgos' },
];

// Etiqueta humana de un nodo. Los errores fuera de nodo (resolución de contexto,
// persistencia) llegan con nodo === null y se muestran como "Preparación".
function labelNodo(nodo: NodoWorkflow | null): string {
  if (nodo === null) return 'Preparación de la auditoría';
  return AGENTES.find((a) => a.nodo === nodo)?.label ?? nodo;
}

const POLL_INTERVAL_MS = 2000;

export default function EjecucionAuditoria({ auditoriaId }: Props) {
  const navigate = useNavigate();
  const [estadoAuditoria, setEstadoAuditoria] = useState<EstadoAuditoria>('EnCurso');
  const [progreso, setProgreso] = useState<ProgresoNodo[]>([]);
  const [errorPolling, setErrorPolling] = useState<string | null>(null);

  // Detalle del fallo (cuando la auditoría termina en Fallida).
  const [mensajeError, setMensajeError] = useState<string | null>(null);
  const [errores, setErrores] = useState<RegistroError[]>([]);

  // Para poder viajar al detalle del proyecto y mostrar el resumen al terminar.
  const [proyectoId, setProyectoId] = useState<number | null>(null);
  const [resultado, setResultado] = useState<AuditoriaResultado | null>(null);

  // Refs para evitar que el effect se re-ejecute por cambios de estado:
  // solo queremos un loop de polling por montaje del componente.
  const cancelado = useRef(false);

  useEffect(() => {
    cancelado.current = false;

    async function poll() {
      while (!cancelado.current) {
        try {
          const [audi, prog] = await Promise.all([
            obtenerAuditoria(auditoriaId),
            obtenerProgreso(auditoriaId),
          ]);

          if (cancelado.current) return;

          setProgreso(prog);
          setEstadoAuditoria(audi.estado);
          setProyectoId(audi.proyectoId);
          setMensajeError(audi.mensajeError);
          setErrorPolling(null);

          // Corte: si la auditoría llegó a estado terminal, salimos del loop.
          // El backend ya marcó los nodos como Completado / Fallido (vía el
          // catch del runner). El render final usa estadoAuditoria.
          if (audi.estado !== 'EnCurso') return;
        } catch (e) {
          // Un fallo puntual de red no debe cortar el polling. Mostramos un
          // banner pero seguimos intentando hasta que la auditoría termine o
          // el componente se desmonte.
          if (cancelado.current) return;
          setErrorPolling(
            e instanceof Error ? e.message : 'Error consultando estado'
          );
        }

        await delay(POLL_INTERVAL_MS);
      }
    }

    poll();

    return () => {
      cancelado.current = true;
    };
  }, [auditoriaId]);

  // Al fallar, traemos el log de errores para mostrar qué falló y en qué nodo.
  useEffect(() => {
    if (estadoAuditoria !== 'Fallida') return;
    let activo = true;
    obtenerErrores(auditoriaId)
      .then((e) => { if (activo) setErrores(e); })
      .catch(() => { /* el detalle es opcional; igual mostramos el mensaje resumen */ });
    return () => { activo = false; };
  }, [estadoAuditoria, auditoriaId]);

  // Al completarse, traemos el resultado para mostrar un resumen.
  useEffect(() => {
    if (estadoAuditoria !== 'Completada') return;
    let activo = true;
    obtenerResultado(auditoriaId)
      .then((r) => { if (activo) { setResultado(r); setProyectoId(r.proyectoId); } })
      .catch(() => { /* el resumen es opcional; igual ofrecemos navegar */ });
    return () => { activo = false; };
  }, [estadoAuditoria, auditoriaId]);

  if (estadoAuditoria === 'Completada') {
    const cumpl = calcCumpl(resultado);
    const cumplCls = cumpl ? (cumpl.pct >= 90 ? 'ok' : cumpl.pct >= 70 ? 'warn' : 'err') : '';
    const RR = 26, CC = 2 * Math.PI * RR;
    return (
      <div className="ea-result">
        <div className="ea-result-head">
          <span className="ea-badge" aria-hidden>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2">
              <path d="m5 13 4 4L19 7" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </span>
          <div>
            <div className="ea-result-title">Auditoría completada</div>
            <div className="ea-result-sub">El análisis terminó correctamente.</div>
          </div>
        </div>

        {resultado && (
          <>
            <div className="ea-result-row">
              <div className="ea-ring">
                <svg width="64" height="64" viewBox="0 0 64 64">
                  <circle className="track" cx="32" cy="32" r={RR} fill="none" strokeWidth="6" />
                  {cumpl && (
                    <circle className={`bar ${cumplCls}`} cx="32" cy="32" r={RR} fill="none" strokeWidth="6"
                      strokeDasharray={CC} strokeDashoffset={CC * (1 - cumpl.pct / 100)} />
                  )}
                </svg>
                <span className="center">{cumpl ? `${cumpl.pct}%` : '—'}</span>
              </div>
              <div>
                <div className="ea-ring-t">Cumplimiento</div>
                <div className="ea-ring-s">
                  {cumpl ? `${cumpl.conf}/${cumpl.ev} artefactos conformes` : 'Sin artefactos con veredicto'}
                </div>
              </div>
            </div>

            <div className="ea-stats">
              <div className="ea-stat"><div className="n">{resultado.contadores.artefactosEvaluados}</div><div className="l">Artefactos evaluados</div></div>
              <div className="ea-stat"><div className="n">{resultado.contadores.hallazgos}</div><div className="l">Hallazgos</div></div>
              <div className="ea-stat"><div className="n">{resultado.contadores.documentosAnalizados}</div><div className="l">Documentos</div></div>
            </div>
          </>
        )}

        <div className="ea-actions">
          <button
            type="button"
            className="btn-pri"
            disabled={proyectoId == null}
            onClick={() => proyectoId != null && navigate(`/proyectos/${proyectoId}`)}
          >
            Ver resultado completo
            <svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m9 6 6 6-6 6" /></svg>
          </button>
          <button type="button" className="btn-sec" onClick={() => navigate('/hallazgos')}>
            Ir a Hallazgos
          </button>
        </div>
      </div>
    );
  }

  if (estadoAuditoria === 'Fallida') {
    return (
      <div className="ea-result">
        <div className="ea-result-head">
          <span className="ea-badge err" aria-hidden>
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2">
              <path d="M10.3 3.86 1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.7 3.86a2 2 0 0 0-3.4 0Z" strokeLinejoin="round" />
              <path d="M12 9v4M12 17h.01" strokeLinecap="round" />
            </svg>
          </span>
          <div>
            <div className="ea-result-title">La auditoría falló</div>
            <div className="ea-result-sub">
              {mensajeError ??
                'Ocurrió un error durante la ejecución. Revisá la configuración del proyecto e intentá de nuevo.'}
            </div>
          </div>
        </div>

        {errores.length > 0 && (
          <ul className="ea-error-list">
            {errores.map((e) => (
              <li key={e.id} className="ea-error-item">
                <span className="ea-error-nodo">{labelNodo(e.nodo)}</span>
                <span className="ea-error-msg">{e.mensaje}</span>
              </li>
            ))}
          </ul>
        )}

        <div className="ea-actions">
          {proyectoId != null && (
            <button type="button" className="btn-sec" onClick={() => navigate(`/proyectos/${proyectoId}`)}>
              Ver detalle del proyecto
            </button>
          )}
        </div>
      </div>
    );
  }

  return (
    <div className="running-box">
      <div className="running-title">⚙️ Auditoría en ejecución...</div>
      <div className="running-sub">Los agentes IA están analizando el proyecto</div>

      {errorPolling && (
        <div className="error-banner" style={{ marginBottom: '1rem' }}>
          {errorPolling} — reintentando…
        </div>
      )}

      <div className="agent-list">
        {AGENTES.map(({ nodo, icono, label }) => {
          const fila = progreso.find((p) => p.nodo === nodo);
          const estado: EstadoNodo = fila?.estado ?? 'Pendiente';
          return (
            <div key={nodo} className="agent-row">
              <span>{icono}</span>
              <span>{label}</span>
              <span className={`agent-status ${estadoClass(estado)}`}>
                {renderEstado(estado)}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}

function estadoClass(estado: EstadoNodo): string {
  switch (estado) {
    case 'EnCurso': return 'encurso';
    case 'Completado': return 'completado';
    case 'Fallido': return 'fallido';
    default: return 'pendiente';
  }
}

function renderEstado(estado: EstadoNodo) {
  switch (estado) {
    case 'EnCurso':
      return (
        <>
          <span className="spin">⟳</span> Procesando…
        </>
      );
    case 'Completado': return '✓ Completado';
    case 'Fallido': return '✗ Falló';
    default: return 'En espera';
  }
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => setTimeout(resolve, ms));
}
