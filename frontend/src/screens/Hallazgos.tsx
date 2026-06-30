// ============================================================================
//  Pantalla principal de Hallazgos (rediseño aprobado por el cliente).
//
//  Criterio: un resultado vacío es ambiguo (¿todo OK o falló la consulta?).
//  Por eso la pantalla:
//   - Muestra un semáforo (rojo/amarillo/verde) + torta de cumplimiento general.
//   - Deja constancia de "se revisaron N ítems" siempre.
//   - Si no hay hallazgos, lo dice explícito en verde (NO pantalla en blanco).
//   - Si la carga falla, muestra un banner de error rojo (distinto del vacío-OK).
//   - Lista los artefactos conformes en un acordeón colapsado.
//
//  Se quitó el campo "Estado" por hallazgo (el cliente prioriza ver la
//  evolución de ejecuciones del proyecto — eso vive en el detalle de proyecto).
//
//  Se conserva la exportación a PDF (exportarHallazgosPdf) y el modal de detalle.
// ============================================================================

import { useEffect, useMemo, useState } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { hallazgosCss } from '../styles/hallazgos';
import { useAuth } from '../login/AuthContext';
import {
  type Hallazgo,
  type TipoHallazgo,
  type ItemConforme,
  type AlcanceRevision,
  TIPO_LABEL,
  cargarRevision,
} from '../api/hallazgos';
import HallazgoDetalleModal from '../components/HallazgoDetalleModal';
import { exportarHallazgosPdf } from '../utils/exportHallazgosPdf';

type FiltroTipo = 'Todos' | TipoHallazgo;

const PAGE_SIZE = 6;

function tipoBadgeClass(tipo: TipoHallazgo): string {
  if (tipo === 'NoConformidad') return 'nc';
  if (tipo === 'Observacion') return 'obs';
  return 'om';
}

const R = 49;
const C = 2 * Math.PI * R;

export default function Hallazgos() {
  useInjectStyle(hallazgosCss, 'hallazgos-style');

  const { usuario } = useAuth();

  const [hallazgos, setHallazgos] = useState<Hallazgo[]>([]);
  const [conformes, setConformes] = useState<ItemConforme[]>([]);
  const [revisados, setRevisados] = useState(0);
  const [totalConf, setTotalConf] = useState(0);
  const [totalNoConf, setTotalNoConf] = useState(0);

  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');

  const [busqueda, setBusqueda] = useState('');
  const [filtro, setFiltro] = useState<FiltroTipo>('Todos');
  const [scope, setScope] = useState<AlcanceRevision>('actual');
  const [pagina, setPagina] = useState(1);

  const [seleccionado, setSeleccionado] = useState<Hallazgo | null>(null);
  const [exportando, setExportando] = useState(false);
  const [conformesAbierto, setConformesAbierto] = useState(false);

  // Carga (re-ejecuta al cambiar el alcance actual/histórico).
  useEffect(() => {
    let activo = true;
    setCargando(true);
    setError('');
    cargarRevision(scope)
      .then((rev) => {
        if (!activo) return;
        setHallazgos(rev.hallazgos);
        setConformes(rev.conformes);
        setRevisados(rev.revisados);
        setTotalConf(rev.totalConformes);
        setTotalNoConf(rev.totalNoConformes);
      })
      .catch((e) => {
        if (activo) setError(e instanceof Error ? e.message : 'No se pudo cargar la revisión.');
      })
      .finally(() => { if (activo) setCargando(false); });
    return () => { activo = false; };
  }, [scope]);

  // Conteos por tipo
  const stats = useMemo(() => ({
    nc:  hallazgos.filter((h) => h.tipo === 'NoConformidad').length,
    obs: hallazgos.filter((h) => h.tipo === 'Observacion').length,
    om:  hallazgos.filter((h) => h.tipo === 'OportunidadMejora').length,
    total: hallazgos.length,
    proyectos: new Set(hallazgos.map((h) => h.proyecto)).size,
  }), [hallazgos]);

  // Semáforo + cumplimiento general
  const semaforo = stats.nc > 0 ? 'rojo' : stats.total > 0 ? 'amarillo' : 'verde';
  const estadoTxt = stats.nc > 0 ? 'Requiere atención' : stats.total > 0 ? 'Con observaciones' : 'Conforme';
  const pctCumpl = revisados > 0 ? Math.round((totalConf / revisados) * 100) : 0;

  // Filtrado
  const hallazgosFiltrados = useMemo(() => {
    let result = hallazgos;
    if (filtro !== 'Todos') result = result.filter((h) => h.tipo === filtro);
    if (busqueda.trim()) {
      const q = busqueda.toLowerCase();
      result = result.filter(
        (h) =>
          h.titulo.toLowerCase().includes(q) ||
          h.descripcion.toLowerCase().includes(q) ||
          h.proyecto.toLowerCase().includes(q) ||
          h.evidencia.toLowerCase().includes(q),
      );
    }
    return result;
  }, [hallazgos, filtro, busqueda]);

  useEffect(() => { setPagina(1); }, [filtro, busqueda]);

  const totalPaginas = Math.max(1, Math.ceil(hallazgosFiltrados.length / PAGE_SIZE));
  const desde = (pagina - 1) * PAGE_SIZE;
  const visibles = hallazgosFiltrados.slice(desde, desde + PAGE_SIZE);

  const handleExport = () => {
    setExportando(true);
    try {
      exportarHallazgosPdf({ hallazgos: hallazgosFiltrados, generadoPor: usuario?.nombre ?? 'Usuario' });
    } catch (err) {
      console.error('Error al generar PDF:', err);
      alert('Hubo un error al generar el informe.');
    } finally {
      setExportando(false);
    }
  };

  // Donut de cumplimiento (conforme vs no conforme)
  const segConf = revisados > 0 ? (totalConf / revisados) * C : 0;

  return (
    <>
      {/* Header */}
      <div className="hz-header">
        <div>
          <h1 className="hz-header-title">Hallazgos</h1>
          <p className="hz-header-sub">
            {scope === 'actual'
              ? 'Última revisión de cada proyecto · estado actual'
              : 'Todas las ejecuciones completadas · histórico'}
          </p>
        </div>
        <div className="hz-header-actions">
          <div className="hz-pills" role="group" aria-label="Alcance">
            {([['actual', 'Actual'], ['historico', 'Histórico']] as [AlcanceRevision, string][]).map(([val, lbl]) => (
              <button
                key={val}
                type="button"
                className={`hz-pill ${scope === val ? 'active' : ''}`}
                onClick={() => setScope(val)}
              >
                {lbl}
              </button>
            ))}
          </div>
          <button
            className="btn-pri"
            onClick={handleExport}
            disabled={exportando || cargando || hallazgos.length === 0}
            type="button"
          >
            Exportar informe
            <svg width="13" height="13" viewBox="0 0 16 16" fill="none">
              <path d="M5 11l6-6M5 5h6v6" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
            </svg>
          </button>
        </div>
      </div>

      {/* Error (distinto del vacío-OK) */}
      {error && (
        <div className="hz-error">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><circle cx="12" cy="12" r="9" /><path d="M12 8v5M12 16h.01" /></svg>
          No se pudo cargar la revisión: {error}
        </div>
      )}

      {/* Banda de estado: semáforo + torta + resumen */}
      {!error && (
        <div className="hz-status">
          <div className={`hz-sem ${semaforo}`}>
            <div className="hz-light-stack"><span className="hz-lt r" /><span className="hz-lt y" /><span className="hz-lt g" /></div>
            <div>
              <div className="hz-sem-estado">{cargando ? 'Revisando…' : estadoTxt}</div>
              <div className="hz-sem-resumen">
                {cargando
                  ? 'Cargando el resultado de las últimas ejecuciones…'
                  : `Se revisaron ${revisados} ítems: ${totalConf} conformes · ${stats.total} con hallazgos`
                    + (stats.nc > 0 ? ` (incluye ${stats.nc} no conformidad${stats.nc > 1 ? 'es' : ''}).`
                       : stats.total > 0 ? ' (sin no conformidades).' : '. Todo en orden.')}
              </div>
            </div>
          </div>

          <div className="hz-donut">
            <svg width="118" height="118" viewBox="0 0 118 118">
              <circle className="track" cx="59" cy="59" r={R} fill="none" strokeWidth="15" />
              {revisados > 0 && (
                <>
                  <circle cx="59" cy="59" r={R} fill="none" stroke="var(--ok-fg)" strokeWidth="15"
                    strokeDasharray={`${segConf.toFixed(2)} ${(C - segConf).toFixed(2)}`} strokeDashoffset="0" />
                  <circle cx="59" cy="59" r={R} fill="none" stroke="var(--err-fg)" strokeWidth="15"
                    strokeDasharray={`${(C - segConf).toFixed(2)} ${segConf.toFixed(2)}`} strokeDashoffset={`${(-segConf).toFixed(2)}`} />
                </>
              )}
            </svg>
            <div className="center"><div className="pct">{pctCumpl}%</div><div className="cap">cumplimiento</div></div>
          </div>

          <div className="hz-dleg">
            <div className="l"><span className="sw" style={{ background: 'var(--ok-fg)' }} />Conforme <b>{totalConf}</b></div>
            <div className="l"><span className="sw" style={{ background: 'var(--err-fg)' }} />No conforme <b>{totalNoConf}</b></div>
          </div>
        </div>
      )}

      {/* Stats por tipo */}
      {!error && (
        <div className="hz-stats">
          <div className="hz-stat-card">
            <div className="hz-stat-label">No conformidades</div>
            <div className="hz-stat-value nc">{stats.nc}</div>
            <div className="hz-stat-meta">bloquean el cumplimiento</div>
          </div>
          <div className="hz-stat-card">
            <div className="hz-stat-label">Observaciones</div>
            <div className="hz-stat-value obs">{stats.obs}</div>
            <div className="hz-stat-meta">a revisar</div>
          </div>
          <div className="hz-stat-card">
            <div className="hz-stat-label">Oportunidades de mejora</div>
            <div className="hz-stat-value om">{stats.om}</div>
            <div className="hz-stat-meta">sugerencias</div>
          </div>
          <div className="hz-stat-card">
            <div className="hz-stat-label">Total hallazgos</div>
            <div className="hz-stat-value">{stats.total}</div>
            <div className="hz-stat-meta">en {stats.proyectos} proyecto{stats.proyectos === 1 ? '' : 's'}</div>
          </div>
        </div>
      )}

      {/* Filtros */}
      {!error && (
        <div className="hz-filters-bar">
          <div className="hz-search">
            <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
              <circle cx="7" cy="7" r="5" stroke="currentColor" strokeWidth="1.4" />
              <path d="M11 11l3 3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
            </svg>
            <input type="text" placeholder="Buscar hallazgo…" value={busqueda} onChange={(e) => setBusqueda(e.target.value)} />
          </div>
          <div className="hz-pills">
            {([['Todos', 'Todos'], ['NoConformidad', 'No conformidades'], ['Observacion', 'Observaciones'], ['OportunidadMejora', 'Oportunidades']] as [FiltroTipo, string][]).map(([val, lbl]) => (
              <button key={val} type="button" className={`hz-pill ${filtro === val ? 'active' : ''}`} onClick={() => setFiltro(val)}>{lbl}</button>
            ))}
          </div>
        </div>
      )}

      {/* Lista de hallazgos / vacío-OK */}
      {!error && (
        <div className="hz-table-card">
          {cargando ? (
            <div className="hz-empty">Cargando hallazgos…</div>
          ) : hallazgos.length === 0 ? (
            <div className="hz-ok-note">
              <div className="ic">✓</div>
              <div>
                <div className="t">No se detectaron hallazgos</div>
                <div className="s">Se revisaron {revisados} ítems y todos resultaron conformes. La revisión corrió correctamente.</div>
              </div>
            </div>
          ) : visibles.length === 0 ? (
            <div className="hz-empty">No hay hallazgos que coincidan con los filtros.</div>
          ) : (
            <>
              <table className="hz-table">
                <thead>
                  <tr>
                    <th style={{ width: '52%' }}>Descripción / Evidencia</th>
                    <th style={{ width: '22%' }}>Proyecto</th>
                    <th style={{ width: '10%' }}>Tipo</th>
                    <th style={{ width: '16%' }}></th>
                  </tr>
                </thead>
                <tbody>
                  {visibles.map((h) => (
                    <tr key={h.id}>
                      <td>
                        <div className="hz-row-title">{h.titulo}</div>
                        <div className="hz-row-meta">{h.evidencia}</div>
                      </td>
                      <td>{h.proyecto}</td>
                      <td><span className={`hz-badge ${tipoBadgeClass(h.tipo)}`}>{TIPO_LABEL[h.tipo]}</span></td>
                      <td><button className="hz-ver-btn" onClick={() => setSeleccionado(h)} type="button">Ver →</button></td>
                    </tr>
                  ))}
                </tbody>
              </table>

              <div className="hz-pagination">
                <span className="hz-pagination-info">Mostrando {visibles.length} de {hallazgosFiltrados.length} hallazgos</span>
                <div className="hz-pagination-pages">
                  {Array.from({ length: totalPaginas }, (_, i) => i + 1).map((n) => (
                    <button key={n} type="button" className={`hz-page-btn ${pagina === n ? 'active' : ''}`} onClick={() => setPagina(n)}>{n}</button>
                  ))}
                </div>
              </div>
            </>
          )}
        </div>
      )}

      {/* Conformes colapsados */}
      {!error && !cargando && conformes.length > 0 && (
        <div className={`hz-acc${conformesAbierto ? ' open' : ''}`} style={{ marginTop: '1.2rem' }}>
          <div className="hz-acc-head" onClick={() => setConformesAbierto((v) => !v)}>
            <svg className="hz-acc-caret" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2"><path d="m9 6 6 6-6 6" /></svg>
            <span className="hz-acc-lbl"><span className="hz-acc-check">✓</span>{conformes.length} artefactos conformes · revisados, sin observaciones</span>
          </div>
          <div className="hz-acc-body">
            <div className="hz-acc-hint">Se listan también los ítems revisados que resultaron OK, para dejar constancia de que la revisión sí corrió.</div>
            {conformes.map((c) => (
              <div key={`${c.proyecto}-${c.id}`} className="hz-conf-row">
                <svg className="ck" viewBox="0 0 24 24" width="15" height="15" fill="none" stroke="currentColor" strokeWidth="2.5"><path d="m5 13 4 4L19 7" /></svg>
                <span>{c.codigo && <code>{c.codigo}</code>}{c.nombre}</span>
                <span className="pj">{c.proyecto}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      {seleccionado && <HallazgoDetalleModal hallazgo={seleccionado} onClose={() => setSeleccionado(null)} />}
    </>
  );
}