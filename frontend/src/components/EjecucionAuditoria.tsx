import { useEffect, useRef, useState } from 'react';
import {
  obtenerAuditoria,
  obtenerProgreso,
  type EstadoAuditoria,
  type EstadoNodo,
  type NodoWorkflow,
  type ProgresoNodo,
} from '../api/auditorias';

interface Props {
  auditoriaId: number;
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

const POLL_INTERVAL_MS = 2000;

export default function EjecucionAuditoria({ auditoriaId }: Props) {
  const [estadoAuditoria, setEstadoAuditoria] = useState<EstadoAuditoria>('EnCurso');
  const [progreso, setProgreso] = useState<ProgresoNodo[]>([]);
  const [errorPolling, setErrorPolling] = useState<string | null>(null);

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

  if (estadoAuditoria === 'Completada') {
    return <div className="final-box">✅ Auditoría completada</div>;
  }

  if (estadoAuditoria === 'Fallida') {
    return (
      <div className="final-box error">
        ⚠️ La auditoría falló durante la ejecución
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
