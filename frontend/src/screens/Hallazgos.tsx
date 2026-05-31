
// ============================================================================
//  Pantalla principal de Hallazgos.
//
//  Funcionalidad:
//   - Carga inicial de los hallazgos via listarHallazgos() (mock por ahora).
//   - 4 tarjetas resumen calculadas dinamicamente.
//   - Buscador por texto (descripcion + proyecto + titulo).
//   - Filtros tipo pill (Todos / NC / OBS / OM / Sin resolver).
//   - Tabla con paginacion (6 hallazgos por pagina).
//   - Modal de detalle al click en "Ver".
//   - Boton "Exportar informe" que genera PDF.
//
//  La pantalla NO sabe que la data es mock — solo conoce `listarHallazgos()`.
//  Cuando el backend exista, se cambia esa funcion y nada mas.
// ============================================================================

import { useEffect, useMemo, useState } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { hallazgosCss } from '../styles/hallazgos';
import { useAuth } from '../login/AuthContext';
import {
  type Hallazgo,
  type TipoHallazgo,
  TIPO_LABEL,
  ESTADO_LABEL,
  listarHallazgos,
} from '../api/hallazgos';
import HallazgoDetalleModal from '../components/HallazgoDetalleModal';
import { exportarHallazgosPdf } from '../utils/exportHallazgosPdf';

type FiltroTipo = 'Todos' | TipoHallazgo | 'SinResolver';

const PAGE_SIZE = 6;

// ── Helpers de presentacion ──────────────────────────────────────────────────

function tipoBadgeClass(tipo: TipoHallazgo): string {
  if (tipo === 'NoConformidad') return 'nc';
  if (tipo === 'Observacion') return 'obs';
  return 'om';
}

function estadoDotClass(estado: Hallazgo['estado']): string {
  if (estado === 'Abierto') return 'estado-abierto';
  if (estado === 'EnRevision') return 'estado-en-revision';
  return 'estado-resuelto';
}

// ── Componente principal ─────────────────────────────────────────────────────

export default function Hallazgos() {
  useInjectStyle(hallazgosCss, 'hallazgos-style');

  const { usuario } = useAuth();

  const [hallazgos, setHallazgos] = useState<Hallazgo[]>([]);
  const [cargando, setCargando]   = useState(true);

  const [busqueda, setBusqueda]   = useState('');
  const [filtro, setFiltro]       = useState<FiltroTipo>('Todos');
  const [pagina, setPagina]       = useState(1);

  const [seleccionado, setSeleccionado] = useState<Hallazgo | null>(null);
  const [exportando, setExportando]     = useState(false);

  // Carga inicial
  useEffect(() => {
    listarHallazgos()
      .then(setHallazgos)
      .finally(() => setCargando(false));
  }, []);

  // ── Stats calculados ────────────────────────────────────────────────────
  const stats = useMemo(() => {
    const nc  = hallazgos.filter((h) => h.tipo === 'NoConformidad');
    const obs = hallazgos.filter((h) => h.tipo === 'Observacion');
    const om  = hallazgos.filter((h) => h.tipo === 'OportunidadMejora');

    const ncAbiertos = nc.filter((h) => h.estado !== 'Resuelto').length;
    const omSinAccion = om.filter((h) => h.estado === 'Abierto').length;

    // "Ultimos 30 dias" — para mock todos califican, pero dejamos la logica
    const haceTreintaDias = Date.now() - 30 * 24 * 60 * 60 * 1000;
    const obsRecientes = obs.filter(
      (h) => new Date(h.fechaDeteccion).getTime() >= haceTreintaDias,
    ).length;

    // Proyectos distintos
    const proyectosUnicos = new Set(hallazgos.map((h) => h.proyecto)).size;

    return {
      nc:  nc.length,
      obs: obs.length,
      om:  om.length,
      total: hallazgos.length,
      ncAbiertos,
      omSinAccion,
      obsRecientes,
      proyectosUnicos,
    };
  }, [hallazgos]);

  // ── Filtrado ────────────────────────────────────────────────────────────
  const hallazgosFiltrados = useMemo(() => {
    let result = hallazgos;

    // Filtro por tipo / sin resolver
    if (filtro === 'NoConformidad' || filtro === 'Observacion' || filtro === 'OportunidadMejora') {
      result = result.filter((h) => h.tipo === filtro);
    } else if (filtro === 'SinResolver') {
      result = result.filter((h) => h.estado !== 'Resuelto');
    }

    // Filtro por busqueda
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

  // Reset de pagina cuando cambian filtros
  useEffect(() => {
    setPagina(1);
  }, [filtro, busqueda]);

  // ── Paginacion ──────────────────────────────────────────────────────────
  const totalPaginas = Math.max(1, Math.ceil(hallazgosFiltrados.length / PAGE_SIZE));
  const desde = (pagina - 1) * PAGE_SIZE;
  const visibles = hallazgosFiltrados.slice(desde, desde + PAGE_SIZE);

  // ── Handlers ────────────────────────────────────────────────────────────
  const handleExport = () => {
    setExportando(true);
    try {
      exportarHallazgosPdf({
        hallazgos: hallazgosFiltrados, // exporta lo que esta filtrado
        generadoPor: usuario?.nombre ?? 'Usuario',
      });
    } catch (err) {
      console.error('Error al generar PDF:', err);
      alert('Hubo un error al generar el informe.');
    } finally {
      setExportando(false);
    }
  };

  return (
    <>
      {/* ── Header ──────────────────────────────────────────────────────── */}
      <div className="hz-header">
        <div>
          <h1 className="hz-header-title">Hallazgos</h1>
          <p className="hz-header-sub">Todos los hallazgos detectados por los agentes IA</p>
        </div>
        <button
          className="hz-export-btn"
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

      {/* ── Stats ───────────────────────────────────────────────────────── */}
      <div className="hz-stats">
        <div className="hz-stat-card">
          <div className="hz-stat-label">No conformidades</div>
          <div className="hz-stat-value nc">{stats.nc}</div>
          <div className="hz-stat-meta">{stats.ncAbiertos} sin resolver</div>
        </div>
        <div className="hz-stat-card">
          <div className="hz-stat-label">Observaciones</div>
          <div className="hz-stat-value obs">{stats.obs}</div>
          <div className="hz-stat-meta">últimos 30 días</div>
        </div>
        <div className="hz-stat-card">
          <div className="hz-stat-label">Oportunidades de mejora</div>
          <div className="hz-stat-value om">{stats.om}</div>
          <div className="hz-stat-meta">{stats.omSinAccion} sin acción asignada</div>
        </div>
        <div className="hz-stat-card">
          <div className="hz-stat-label">Total hallazgos</div>
          <div className="hz-stat-value">{stats.total}</div>
          <div className="hz-stat-meta">en {stats.proyectosUnicos} proyectos</div>
        </div>
      </div>

      {/* ── Filtros ─────────────────────────────────────────────────────── */}
      <div className="hz-filters-bar">
        <div className="hz-search">
          <svg width="14" height="14" viewBox="0 0 16 16" fill="none">
            <circle cx="7" cy="7" r="5" stroke="currentColor" strokeWidth="1.4" />
            <path d="M11 11l3 3" stroke="currentColor" strokeWidth="1.4" strokeLinecap="round" />
          </svg>
          <input
            type="text"
            placeholder="Buscar hallazgo…"
            value={busqueda}
            onChange={(e) => setBusqueda(e.target.value)}
          />
        </div>

        <div className="hz-pills">
          <button
            type="button"
            className={`hz-pill ${filtro === 'Todos' ? 'active' : ''}`}
            onClick={() => setFiltro('Todos')}
          >
            Todos
          </button>
          <button
            type="button"
            className={`hz-pill ${filtro === 'NoConformidad' ? 'active' : ''}`}
            onClick={() => setFiltro('NoConformidad')}
          >
            No conformidades
          </button>
          <button
            type="button"
            className={`hz-pill ${filtro === 'Observacion' ? 'active' : ''}`}
            onClick={() => setFiltro('Observacion')}
          >
            Observaciones
          </button>
          <button
            type="button"
            className={`hz-pill ${filtro === 'OportunidadMejora' ? 'active' : ''}`}
            onClick={() => setFiltro('OportunidadMejora')}
          >
            Oportunidades
          </button>
          <button
            type="button"
            className={`hz-pill ${filtro === 'SinResolver' ? 'active' : ''}`}
            onClick={() => setFiltro('SinResolver')}
          >
            Sin resolver
          </button>
        </div>
      </div>

      {/* ── Tabla ───────────────────────────────────────────────────────── */}
      <div className="hz-table-card">
        {cargando ? (
          <div className="hz-empty">Cargando hallazgos…</div>
        ) : visibles.length === 0 ? (
          <div className="hz-empty">No hay hallazgos que coincidan con los filtros.</div>
        ) : (
          <>
            <table className="hz-table">
              <thead>
                <tr>
                  <th style={{ width: '45%' }}>Descripción / Evidencia</th>
                  <th style={{ width: '18%' }}>Proyecto</th>
                  <th style={{ width: '8%' }}>Tipo</th>
                  <th style={{ width: '15%' }}>Estado</th>
                  <th style={{ width: '14%' }}></th>
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
                    <td>
                      <span className={`hz-badge ${tipoBadgeClass(h.tipo)}`}>
                        {TIPO_LABEL[h.tipo]}
                      </span>
                    </td>
                    <td>
                      <span className={`hz-estado-dot ${estadoDotClass(h.estado)}`}>
                        {ESTADO_LABEL[h.estado]}
                      </span>
                    </td>
                    <td>
                      <button
                        className="hz-ver-btn"
                        onClick={() => setSeleccionado(h)}
                        type="button"
                      >
                        Ver →
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>

            {/* Paginacion */}
            <div className="hz-pagination">
              <span className="hz-pagination-info">
                Mostrando {visibles.length} de {hallazgosFiltrados.length} hallazgos
              </span>
              <div className="hz-pagination-pages">
                {Array.from({ length: totalPaginas }, (_, i) => i + 1).map((n) => (
                  <button
                    key={n}
                    type="button"
                    className={`hz-page-btn ${pagina === n ? 'active' : ''}`}
                    onClick={() => setPagina(n)}
                  >
                    {n}
                  </button>
                ))}
              </div>
            </div>
          </>
        )}
      </div>

      {/* ── Modal de detalle ────────────────────────────────────────────── */}
      {seleccionado && (
        <HallazgoDetalleModal
          hallazgo={seleccionado}
          onClose={() => setSeleccionado(null)}
        />
      )}
    </>
  );
}