import { useEffect, useState } from 'react';
import { listarProyectos, type Proyecto } from '../api/proyectos';
import { listarEtapasDeProcedimiento, type Etapa } from '../api/procedimientos';
import { crearAuditoria } from '../api/auditorias';
import EjecucionAuditoria from '../components/EjecucionAuditoria';

// Pantalla "Nueva auditoría". Tiene tres fases:
//   1. Carga: trae proyectos del backend para poblar el select.
//   2. Selección: usuario elige Proyecto → se cargan Etapas del procedimiento
//      de ese proyecto → elige Etapa → habilita el botón.
//   3. Ejecución: POST a /api/auditorias. A partir de tener auditoriaId,
//      delega en <EjecucionAuditoria> que polea progreso + estado.
//
// Tipo de auditoría, Alcance / Observaciones y Fuentes de datos NO se
// incluyen por decisión explícita: las fuentes las decide el flujo según el
// tailoring, no el usuario.
export default function NuevaAuditoria() {
  const [proyectos, setProyectos] = useState<Proyecto[]>([]);
  const [etapas, setEtapas] = useState<Etapa[]>([]);

  const [proyectoId, setProyectoId] = useState<number | ''>('');
  const [etapaId, setEtapaId] = useState<number | ''>('');

  const [cargandoProyectos, setCargandoProyectos] = useState(true);
  const [cargandoEtapas, setCargandoEtapas] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const [auditoriaId, setAuditoriaId] = useState<number | null>(null);
  const [creando, setCreando] = useState(false);

  // 1. Cargar proyectos al montar.
  useEffect(() => {
    listarProyectos()
      .then((data) => setProyectos(data))
      .catch((e) =>
        setError(e instanceof Error ? e.message : 'Error cargando proyectos')
      )
      .finally(() => setCargandoProyectos(false));
  }, []);

  // 2. Cuando cambia el proyecto seleccionado, recargar etapas del procedimiento
  //    de ese proyecto. Reset de la etapa elegida porque puede no pertenecer al
  //    nuevo procedimiento (en el MVP siempre es PR 11-13, pero el modelo está
  //    preparado para múltiples procedimientos).
  useEffect(() => {
    setEtapaId('');
    setEtapas([]);

    if (proyectoId === '') return;

    const proyecto = proyectos.find((p) => p.id === proyectoId);
    if (!proyecto) return;

    setCargandoEtapas(true);
    listarEtapasDeProcedimiento(proyecto.procedimientoId)
      .then((data) => setEtapas(data))
      .catch((e) =>
        setError(e instanceof Error ? e.message : 'Error cargando etapas')
      )
      .finally(() => setCargandoEtapas(false));
  }, [proyectoId, proyectos]);

  async function ejecutar() {
    if (proyectoId === '' || etapaId === '') return;

    setCreando(true);
    setError(null);

    try {
      const { auditoriaId } = await crearAuditoria(
        Number(proyectoId),
        Number(etapaId)
      );
      setAuditoriaId(auditoriaId);
    } catch (e) {
      setError(
        e instanceof Error ? e.message : 'No se pudo crear la auditoría'
      );
    } finally {
      setCreando(false);
    }
  }

  const proyectoSel = proyectos.find((p) => p.id === proyectoId);
  const etapaSel = etapas.find((e) => e.id === etapaId);
  const puedeEjecutar = proyectoId !== '' && etapaId !== '' && !creando;

  return (
    <>
      <div className="topbar">
        <div>
          <div className="page-title">Nueva auditoría</div>
          <div className="page-sub">
            Configurá y ejecutá una auditoría sobre un proyecto
          </div>
        </div>
      </div>

      {error && <div className="error-banner">{error}</div>}

      {auditoriaId === null ? (
        <>
          <div className="card">
            <div className="card-title">
              <svg viewBox="0 0 16 16" fill="none">
                <path d="M2 4h12M2 8h12M2 12h7" stroke="currentColor" strokeWidth="1.2" strokeLinecap="round" />
              </svg>
              Selección de proyecto
            </div>
            <div className="field-row">
              <div className="field">
                <label>Proyecto a auditar</label>
                <select
                  value={proyectoId}
                  disabled={cargandoProyectos}
                  onChange={(e) =>
                    setProyectoId(e.target.value === '' ? '' : Number(e.target.value))
                  }
                >
                  <option value="">
                    {cargandoProyectos ? 'Cargando…' : 'Elegí un proyecto'}
                  </option>
                  {proyectos.map((p) => (
                    <option key={p.id} value={p.id}>
                      {p.nombre}
                    </option>
                  ))}
                </select>
              </div>
              <div className="field">
                <label>Etapa</label>
                <select
                  value={etapaId}
                  disabled={proyectoId === '' || cargandoEtapas}
                  onChange={(e) =>
                    setEtapaId(e.target.value === '' ? '' : Number(e.target.value))
                  }
                >
                  <option value="">
                    {proyectoId === ''
                      ? 'Elegí primero un proyecto'
                      : cargandoEtapas
                      ? 'Cargando…'
                      : 'Elegí una etapa'}
                  </option>
                  {etapas.map((e) => (
                    <option key={e.id} value={e.id}>
                      {e.nombre}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="card-title">
              <svg viewBox="0 0 16 16" fill="none">
                <path d="M13 4L6 11l-3-3" stroke="currentColor" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
              </svg>
              Resumen antes de ejecutar
            </div>
            <div className="resumen-box">
              <div className="res-row">
                <span className="res-key">Proyecto</span>
                <span className={`res-val${proyectoSel ? '' : ' placeholder'}`}>
                  {proyectoSel?.nombre ?? 'Sin seleccionar'}
                </span>
              </div>
              <div className="res-row">
                <span className="res-key">Etapa</span>
                <span className={`res-val${etapaSel ? '' : ' placeholder'}`}>
                  {etapaSel?.nombre ?? 'Sin seleccionar'}
                </span>
              </div>
            </div>

            <div className="btn-row">
              <button
                className="btn-pri"
                disabled={!puedeEjecutar}
                onClick={ejecutar}
              >
                {creando ? 'Iniciando…' : '▶ Ejecutar auditoría'}
              </button>
            </div>
          </div>
        </>
      ) : (
        <div className="card">
          <EjecucionAuditoria auditoriaId={auditoriaId} />
        </div>
      )}
    </>
  );
}
