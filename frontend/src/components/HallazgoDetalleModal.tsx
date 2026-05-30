
// ============================================================================
//  Modal de detalle de un hallazgo.
//  Se abre con ESC para cerrar, click fuera para cerrar, click en X tambien.
// ============================================================================

import { useEffect } from 'react';
import {
  type Hallazgo,
  TIPO_LABEL,
  TIPO_LABEL_LARGO,
  ESTADO_LABEL,
} from '../api/hallazgos';

interface Props {
  hallazgo: Hallazgo;
  onClose: () => void;
}

export default function HallazgoDetalleModal({ hallazgo, onClose }: Props) {
  // Cerrar con ESC
  useEffect(() => {
    const handler = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onClose();
    };
    window.addEventListener('keydown', handler);
    return () => window.removeEventListener('keydown', handler);
  }, [onClose]);

  // Map de tipo a clase de badge
  const tipoClase =
    hallazgo.tipo === 'NoConformidad' ? 'nc' :
    hallazgo.tipo === 'Observacion' ? 'obs' : 'om';

  const estadoClase =
    hallazgo.estado === 'Abierto' ? 'estado-abierto' :
    hallazgo.estado === 'EnRevision' ? 'estado-en-revision' : 'estado-resuelto';

  const fechaFormateada = new Date(hallazgo.fechaDeteccion).toLocaleDateString('es-AR', {
    day: '2-digit', month: '2-digit', year: 'numeric',
    hour: '2-digit', minute: '2-digit',
  });

  return (
    <div className="hz-modal-overlay" onClick={onClose} role="dialog" aria-modal="true">
      <div className="hz-modal" onClick={(e) => e.stopPropagation()}>
        <div className="hz-modal-header">
          <h2 className="hz-modal-title">{hallazgo.titulo}</h2>
          <button
            className="hz-modal-close"
            onClick={onClose}
            aria-label="Cerrar"
            type="button"
          >
            ×
          </button>
        </div>

        <div className="hz-modal-body">
          <div className="hz-modal-tags">
            <span className={`hz-badge ${tipoClase}`}>{TIPO_LABEL[hallazgo.tipo]}</span>
            <span className={`hz-estado-dot ${estadoClase}`}>{ESTADO_LABEL[hallazgo.estado]}</span>
          </div>

          <div className="hz-modal-row">
            <span className="hz-modal-label">Descripción</span>
            <span className="hz-modal-value">{hallazgo.descripcion}</span>
          </div>

          <div className="hz-modal-row">
            <span className="hz-modal-label">Evidencia</span>
            <span className="hz-modal-value">{hallazgo.evidencia}</span>
          </div>

          <div className="hz-modal-row">
            <span className="hz-modal-label">Proyecto</span>
            <span className="hz-modal-value">{hallazgo.proyecto}</span>
          </div>

          <div className="hz-modal-row">
            <span className="hz-modal-label">Tipo</span>
            <span className="hz-modal-value">{TIPO_LABEL_LARGO[hallazgo.tipo]}</span>
          </div>

          <div className="hz-modal-row">
            <span className="hz-modal-label">Fecha de detección</span>
            <span className="hz-modal-value">{fechaFormateada}</span>
          </div>

          {hallazgo.agenteOrigen && (
            <div className="hz-modal-row">
              <span className="hz-modal-label">Agente origen</span>
              <span className="hz-modal-value">{hallazgo.agenteOrigen}</span>
            </div>
          )}

          {hallazgo.justificacion && (
            <div className="hz-modal-row">
              <span className="hz-modal-label">Justificación</span>
              <span className="hz-modal-value">{hallazgo.justificacion}</span>
            </div>
          )}
        </div>
      </div>
    </div>
  );
}