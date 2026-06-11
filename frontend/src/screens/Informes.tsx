// src/screens/Informes.tsx
// ============================================================================
//  Pantalla de Informes.
//
//  Lista los informes YA generados (de auditorías hechas), para volver a verlos
//  o descargarlos en PDF. Si existe un informe es porque la auditoría terminó,
//  así que no hay "pendientes" acá.
//
//  Visibilidad por rol:
//   - Administrador / Auditor → GET /api/informes (todos)
//   - Operador                → solo los informes de sus proyectos asignados,
//     armados a partir de (proyectos → auditorías → informes).
// ============================================================================
import { useEffect, useMemo, useState } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { informesCss } from '../styles/informes';
import { descargarInformePdf } from '../utils/exportInformePdf';
import { useAuth } from '../login/AuthContext';
import {
  listarInformes,
  listarInformesDeAuditoria,
  TIPO_INFORME_LABEL,
  type Informe,
} from '../api/informes';
import { listarProyectos } from '../api/proyectos';
import { listarAuditoriasDeProyecto } from '../api/auditorias';

type Filtro = 'Todos' | 'Auto' | 'Manual';

function fmtFecha(iso: string): string {
  const d = new Date(iso);
  return isNaN(d.getTime()) ? '—' : d.toLocaleDateString('es-AR');
}

function tipoLabel(tipo: string): string {
  return TIPO_INFORME_LABEL[tipo] ?? tipo;
}

export default function Informes() {
  useInjectStyle(informesCss, 'informes-style');

  const { usuario } = useAuth();
  const verTodos = usuario?.rol === 'Administrador' || usuario?.rol === 'Auditor';

  const [informes, setInformes] = useState<Informe[]>([]);
  const [cargando, setCargando] = useState(true);
  const [error, setError]       = useState('');
  const [filtro, setFiltro]     = useState<Filtro>('Todos');
  const [verInforme, setVerInforme] = useState<Informe | null>(null);

  useEffect(() => {
    let activo = true;

    const cargar = async () => {
      try {
        setCargando(true);
        setError('');

        let data: Informe[];
        if (verTodos) {
          // Admin / Auditor: un solo llamado trae todos.
          data = await listarInformes();
        } else {
          // Operador: armamos desde sus proyectos asignados.
          const proyectos = await listarProyectos();
          const auditorias = (
            await Promise.all(proyectos.map((p) => listarAuditoriasDeProyecto(p.id)))
          ).flat();
          const listas = await Promise.all(
            auditorias.map((a) => listarInformesDeAuditoria(a.id)),
          );
          // Dedupe por id (una auditoría podría aparecer en varias listas).
          const mapa = new Map<number, Informe>();
          listas.flat().forEach((i) => mapa.set(i.id, i));
          data = [...mapa.values()];
        }

        // Más recientes primero.
        data.sort((a, b) => +new Date(b.fechaGeneracion) - +new Date(a.fechaGeneracion));
        if (activo) setInformes(data);
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Error al cargar los informes.';
        if (activo) setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
      } finally {
        if (activo) setCargando(false);
      }
    };

    cargar();
    return () => { activo = false; };
  }, [verTodos]);

  const visibles = useMemo(() => {
    if (filtro === 'Todos') return informes;
    return informes.filter((i) => i.tipo === filtro);
  }, [informes, filtro]);

  return (
    <>
      <div className="topbar">
        <div>
          <h1 className="page-title">Informes</h1>
          <p className="page-sub">Informes de auditorías finalizadas · vé el detalle o descargá el PDF</p>
        </div>
      </div>

      <div className="in-filters">
        {(['Todos', 'Auto', 'Manual'] as Filtro[]).map((f) => (
          <button
            key={f}
            className={`in-pill${filtro === f ? ' active' : ''}`}
            onClick={() => setFiltro(f)}
          >
            {f === 'Todos' ? 'Todos' : tipoLabel(f)}
          </button>
        ))}
      </div>

      {error && <div className="error-banner">{error}</div>}

      {cargando ? (
        <p className="loading-text">Cargando informes…</p>
      ) : visibles.length === 0 ? (
        <div className="in-empty">
          {usuario?.rol === 'Operador'
            ? 'Todavía no hay informes de tus proyectos asignados.'
            : 'No hay informes generados todavía.'}
        </div>
      ) : (
        <div className="in-table-wrap">
          <table className="in-table">
            <thead>
              <tr>
                <th>Proyecto</th>
                <th>Fecha de generación</th>
                <th>Tipo</th>
                <th>Estado</th>
                <th style={{ textAlign: 'right' }}>Acción</th>
              </tr>
            </thead>
            <tbody>
              {visibles.map((inf) => (
                <tr key={inf.id}>
                  <td className="in-pj">{inf.nombreProyecto}</td>
                  <td>{fmtFecha(inf.fechaGeneracion)}</td>
                  <td><span className="in-badge tipo">{tipoLabel(inf.tipo)}</span></td>
                  <td><span className="in-badge gen">Generado</span></td>
                  <td>
                    <div className="in-acts">
                      <button className="in-link" onClick={() => setVerInforme(inf)}>
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                          <path d="M2 12s3.5-7 10-7 10 7 10 7-3.5 7-10 7-10-7-10-7z" /><circle cx="12" cy="12" r="3" />
                        </svg>
                        Ver
                      </button>
                      <button className="in-link" onClick={() => descargarInformePdf(inf)}>
                        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8">
                          <path d="M12 3v12M7 10l5 5 5-5M5 21h14" />
                        </svg>
                        PDF
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* ── Modal vista previa ── */}
      {verInforme && (
        <div className="in-overlay" onClick={() => setVerInforme(null)}>
          <div className="in-modal" onClick={(e) => e.stopPropagation()}>
            <div className="in-modal-head">
              <div>
                <div className="in-modal-title">{verInforme.nombreProyecto}</div>
                <div className="in-modal-sub">
                  {tipoLabel(verInforme.tipo)} · {fmtFecha(verInforme.fechaGeneracion)}
                </div>
              </div>
              <button className="in-modal-close" onClick={() => setVerInforme(null)} aria-label="Cerrar">×</button>
            </div>
            <div className="in-modal-body">
              <div className="in-contenido">{verInforme.contenido || '(sin contenido)'}</div>
            </div>
            <div className="in-modal-foot">
              <button className="btn-pri" onClick={() => descargarInformePdf(verInforme)}>
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.8" width="15" height="15">
                  <path d="M12 3v12M7 10l5 5 5-5M5 21h14" />
                </svg>
                Descargar PDF
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}