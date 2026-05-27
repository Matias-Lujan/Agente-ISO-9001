import React, { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import './AuditoriaDashboardScreen.css';

const AuditoriaDashboardScreen = () => {
  const { id } = useParams();
  const navigate = useNavigate();
  const [auditoriaData, setAuditoriaData] = useState(null);

  // Datos mockeados para el dashboard
  const dataMockeada = {
    auditoriaId: id,
    proyecto: 'Proyecto Alpha',
    etapa: 'Etapa 2 - Implementación',
    fechaEjecucion: '2026-05-26',
    estado: 'Completada',
    metricas: {
      totalHallazgos: 15,
      nivelesConformidad: {
        conforme: 8,
        noConforme: 4,
        noEvaluado: 3
      },
      hallazgosNoConformidad: 4,
      hallazgosObservaciones: 7,
      hallazgosMejora: 4,
      nivelCumplimiento: 73
    },
    resumenResultados: [
      {
        id: 1,
        artefacto: 'Manual de Calidad',
        estado: 'Conforme',
        hallazgos: 1,
        prioridad: 'Media'
      },
      {
        id: 2,
        artefacto: 'Procedimiento de RH',
        estado: 'No Conforme',
        hallazgos: 3,
        prioridad: 'Alta'
      },
      {
        id: 3,
        artefacto: 'Registro de Auditorías',
        estado: 'Conforme',
        hallazgos: 0,
        prioridad: 'Baja'
      },
      {
        id: 4,
        artefacto: 'Plan de Acciones Correctivas',
        estado: 'No Conforme',
        hallazgos: 2,
        prioridad: 'Alta'
      },
      {
        id: 5,
        artefacto: 'Procedimiento de Compras',
        estado: 'Conforme',
        hallazgos: 1,
        prioridad: 'Media'
      }
    ]
  };

  useEffect(() => {
    // Simular carga de datos del backend
    setTimeout(() => {
      setAuditoriaData(dataMockeada);
    }, 500);
  }, [id]);

  const getPrioridadBadgeClass = (prioridad) => {
    const classes = {
      'Alta': 'priority-alta',
      'Media': 'priority-media',
      'Baja': 'priority-baja'
    };
    return classes[prioridad] || '';
  };

  const getEstadoBadgeClass = (estado) => {
    const classes = {
      'Conforme': 'estado-conforme',
      'No Conforme': 'estado-no-conforme',
      'No Evaluado': 'estado-no-evaluado'
    };
    return classes[estado] || '';
  };

  if (!auditoriaData) {
    return (
      <div className="dashboard-loading">
        <div className="spinner-dashboard"></div>
        <p>Cargando dashboard...</p>
      </div>
    );
  }

  return (
    <div className="auditoria-dashboard-screen">
      <header className="dashboard-header">
        <h1>Dashboard de Auditoría ISO 9001</h1>
        <p>Análisis Detallado de Cumplimiento</p>
      </header>

      <main className="dashboard-content">
        {/* SECCIÓN: INFORMACIÓN GENERAL */}
        <section className="dashboard-section info-section">
          <div className="info-card">
            <h2>ID Auditoría</h2>
            <p className="info-value">{auditoriaData.auditoriaId}</p>
          </div>
          <div className="info-card">
            <h2>Proyecto</h2>
            <p className="info-value">{auditoriaData.proyecto}</p>
          </div>
          <div className="info-card">
            <h2>Etapa</h2>
            <p className="info-value">{auditoriaData.etapa}</p>
          </div>
          <div className="info-card">
            <h2>Fecha Ejecución</h2>
            <p className="info-value">{auditoriaData.fechaEjecucion}</p>
          </div>
        </section>

        {/* SECCIÓN: MÉTRICAS PRINCIPALES */}
        <section className="dashboard-section metrics-section">
          <h2 className="section-title">Métricas de Cumplimiento</h2>
          
          <div className="metrics-grid">
            <div className="metric-card metric-primary">
              <div className="metric-icon">📊</div>
              <div className="metric-content">
                <p className="metric-label">Nivel de Cumplimiento</p>
                <p className="metric-value">{auditoriaData.metricas.nivelCumplimiento}%</p>
              </div>
              <div className="metric-bar">
                <div 
                  className="metric-bar-fill" 
                  style={{ width: `${auditoriaData.metricas.nivelCumplimiento}%` }}
                ></div>
              </div>
            </div>

            <div className="metric-card metric-success">
              <div className="metric-icon">✓</div>
              <div className="metric-content">
                <p className="metric-label">Conformes</p>
                <p className="metric-value">{auditoriaData.metricas.nivelesConformidad.conforme}</p>
              </div>
            </div>

            <div className="metric-card metric-warning">
              <div className="metric-icon">⚠</div>
              <div className="metric-content">
                <p className="metric-label">No Conformes</p>
                <p className="metric-value">{auditoriaData.metricas.nivelesConformidad.noConforme}</p>
              </div>
            </div>

            <div className="metric-card metric-info">
              <div className="metric-icon">ℹ</div>
              <div className="metric-content">
                <p className="metric-label">No Evaluados</p>
                <p className="metric-value">{auditoriaData.metricas.nivelesConformidad.noEvaluado}</p>
              </div>
            </div>
          </div>

          <div className="metrics-secondary">
            <div className="secondary-metric">
              <span className="secondary-label">Total Hallazgos:</span>
              <span className="secondary-value">{auditoriaData.metricas.totalHallazgos}</span>
            </div>
            <div className="secondary-metric">
              <span className="secondary-label">No Conformidades:</span>
              <span className="secondary-value nc">{auditoriaData.metricas.hallazgosNoConformidad}</span>
            </div>
            <div className="secondary-metric">
              <span className="secondary-label">Observaciones:</span>
              <span className="secondary-value obs">{auditoriaData.metricas.hallazgosObservaciones}</span>
            </div>
            <div className="secondary-metric">
              <span className="secondary-label">Mejoras:</span>
              <span className="secondary-value om">{auditoriaData.metricas.hallazgosMejora}</span>
            </div>
          </div>
        </section>

        {/* SECCIÓN: RESUMEN DE RESULTADOS */}
        <section className="dashboard-section results-section">
          <h2 className="section-title">Resumen de Resultados por Artefacto</h2>
          
          <div className="table-responsive">
            <table className="dashboard-table">
              <thead>
                <tr>
                  <th>ID</th>
                  <th>Artefacto</th>
                  <th>Estado</th>
                  <th>Hallazgos</th>
                  <th>Prioridad</th>
                </tr>
              </thead>
              <tbody>
                {auditoriaData.resumenResultados.map((resultado) => (
                  <tr key={resultado.id} className="resultado-row">
                    <td className="cell-id">{resultado.id}</td>
                    <td className="cell-artefacto">{resultado.artefacto}</td>
                    <td>
                      <span className={`badge ${getEstadoBadgeClass(resultado.estado)}`}>
                        {resultado.estado}
                      </span>
                    </td>
                    <td className="cell-hallazgos">{resultado.hallazgos}</td>
                    <td>
                      <span className={`badge priority-badge ${getPrioridadBadgeClass(resultado.prioridad)}`}>
                        {resultado.prioridad}
                      </span>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>

        {/* SECCIÓN: ACCIONES */}
        <section className="dashboard-section actions-section">
          <button onClick={() => navigate('/')} className="btn-volver">
            ← Volver a Auditoría
          </button>
          <button className="btn-exportar">
            ↓ Exportar Reporte
          </button>
        </section>
      </main>

      <footer className="dashboard-footer">
        <p>&copy; 2026 Sistema de Auditoría ISO 9001 | Dashboard v1.0</p>
      </footer>
    </div>
  );
};

export default AuditoriaDashboardScreen;
