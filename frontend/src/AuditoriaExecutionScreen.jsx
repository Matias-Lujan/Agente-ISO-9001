import React, { useState, useEffect } from 'react';
import './AuditoriaExecutionScreen.css';

const AuditoriaExecutionScreen = () => {
  // Estados de control de fase
  const [status, setStatus] = useState('form'); // 'form', 'loading', 'results'
  const [proyectoSeleccionado, setProyectoSeleccionado] = useState('');
  const [etapaSeleccionada, setEtapaSeleccionada] = useState('');
  
  // Estados de resultados y polling
  const [resultados, setResultados] = useState(null);
  const [error, setError] = useState(null);
  const [auditoriaId, setAuditoriaId] = useState(null);
  
  // URL base del backend (ajusta según tu configuración)
  const API_BASE_URL = 'http://localhost:5180/api';

  // Datos mockeados para proyectos y etapas
  const proyectos = [
    { id: '1', nombre: 'Proyecto Alpha' },
    { id: '2', nombre: 'Proyecto Beta' },
    { id: '3', nombre: 'Proyecto Gamma' }
  ];

  const etapas = [
    { id: '1', nombre: 'Etapa 1 - Planificación' },
    { id: '2', nombre: 'Etapa 2 - Implementación' },
    { id: '3', nombre: 'Etapa 3 - Validación' }
  ];

  // Datos mockeados de resultados
  const resultadosMockeados = {
    auditoriaId: 100,
    proyectoId: parseInt(proyectoSeleccionado),
    etapaId: parseInt(etapaSeleccionada),
    artefactosEvaluados: [
      {
        artefactoEsperadoId: 1,
        nombreArtefacto: 'Manual de Calidad',
        estadoAplicacionTailoring: 'Aplica',
        resultado: 'Conforme',
        justificacionNoAplica: ''
      },
      {
        artefactoEsperadoId: 2,
        nombreArtefacto: 'Procedimiento de RH',
        estadoAplicacionTailoring: 'No Aplica',
        resultado: 'No Evaluado',
        justificacionNoAplica: 'No es aplicable en esta etapa del proyecto'
      },
      {
        artefactoEsperadoId: 3,
        nombreArtefacto: 'Registro de Auditorías',
        estadoAplicacionTailoring: 'Aplica',
        resultado: 'No Conforme',
        justificacionNoAplica: ''
      }
    ],
    hallazgos: [
      {
        id: 'H001',
        artefactoEsperadoId: 3,
        tipo: 'NC',
        descripcion: 'Falta firma del auditor en registro de auditoría del 15/05/2026',
        agenteOrigen: 'DocumentAnalysis',
        fechaDeteccion: '2026-05-25'
      },
      {
        id: 'H002',
        artefactoEsperadoId: 1,
        tipo: 'OBS',
        descripcion: 'Manual de Calidad desactualizado. Última revisión: 2024',
        agenteOrigen: 'ConsistencyVerification',
        fechaDeteccion: '2026-05-25'
      },
      {
        id: 'H003',
        artefactoEsperadoId: 1,
        tipo: 'OM',
        descripcion: 'Se sugiere incluir referencias a normativa ISO 9001:2015 en el manual',
        agenteOrigen: 'ComplianceValidation',
        fechaDeteccion: '2026-05-25'
      }
    ],
    documentosAnalizados: [
      {
        id: 'DOC001',
        nombreArchivo: 'manual-calidad-2024.pdf',
        fuente: 'SharePoint - Calidad',
        hashContenido: 'a3f2b8c1d9e4f7a6b5c2d9e1f4a7b8c',
        tamaño: '2.4 MB',
        fechaCarga: '2026-05-20'
      },
      {
        id: 'DOC002',
        nombreArchivo: 'registros-auditoria-q2-2026.xlsx',
        fuente: 'Teams - Auditoría',
        hashContenido: 'f7a6b5c2d9e1f4a7b8c3d9e1f4a7b8c',
        tamaño: '0.8 MB',
        fechaCarga: '2026-05-22'
      },
      {
        id: 'DOC003',
        nombreArchivo: 'procedimiento-rh-draft.docx',
        fuente: 'OneDrive - RRHH',
        hashContenido: 'd9e1f4a7b8c3d9e1f4a7b8c3d9e1f4a',
        tamaño: '1.2 MB',
        fechaCarga: '2026-05-19'
      }
    ]
  };

  // Manejador para ejecutar auditoría
  const handleEjecutarAuditoria = async () => {
    if (!proyectoSeleccionado || !etapaSeleccionada) {
      setError('Por favor selecciona un proyecto y una etapa');
      return;
    }

    setError(null);
    setStatus('loading');

    try {
      // POST a /api/auditorias para iniciar la auditoría
      const response = await fetch(`${API_BASE_URL}/auditorias`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          auditoriaId: 0,
          proyectoId: parseInt(proyectoSeleccionado),
          etapaId: parseInt(etapaSeleccionada)
        })
      });

      if (!response.ok) {
        throw new Error(`Error al iniciar auditoría: ${response.statusText}`);
      }

      // Esperamos HTTP 202 (Accepted)
      const data = await response.json();
      setAuditoriaId(data.id); // Guardar el ID para el polling
    } catch (err) {
      setError(`Error: ${err.message}`);
      setStatus('form');
    }
  };

  // useEffect para manejar el polling cuando se obtiene un auditoriaId
  useEffect(() => {
    if (!auditoriaId) return; // Si no hay ID, no hacer nada

    let intervalId = null;
    let isMounted = true; // Para evitar memory leaks

    const poll = async () => {
      try {
        const response = await fetch(`${API_BASE_URL}/auditorias/${auditoriaId}`);

        if (!response.ok) {
          throw new Error(`Error al obtener estado: ${response.statusText}`);
        }

        const data = await response.json();

        if (!isMounted) return; // Verificar si el componente sigue montado

        if (data.estado === 'EnCurso') {
          // Continuar polling
          console.log('Auditoría en curso...');
        } else if (data.estado === 'Fallida') {
          // Detener polling y mostrar error
          if (intervalId) clearInterval(intervalId);
          setError(data.mensaje || 'La auditoría falló.');
          setStatus('form');
          setAuditoriaId(null);
        } else if (data.estado === 'Completada') {
          // Detener polling y mostrar resultados
          if (intervalId) clearInterval(intervalId);
          setResultados(data);
          setStatus('results');
          setAuditoriaId(null);
        }
      } catch (err) {
        if (isMounted) {
          setError(`Error en polling: ${err.message}`);
          if (intervalId) clearInterval(intervalId);
          setStatus('form');
          setAuditoriaId(null);
        }
      }
    };

    // Realizar la primera consulta inmediatamente
    poll();

    // Configurar intervalo para las siguientes consultas cada 5 segundos
    intervalId = setInterval(poll, 5000);

    // Cleanup: detener el polling cuando el componente se desmonta o auditoriaId cambia
    return () => {
      isMounted = false;
      if (intervalId) clearInterval(intervalId);
    };
  }, [auditoriaId]);

  // Manejador para reiniciar
  const handleReiniciar = () => {
    setStatus('form');
    setProyectoSeleccionado('');
    setEtapaSeleccionada('');
    setResultados(null);
    setError(null);
    setAuditoriaId(null);
  };

  // Obtener etiqueta visual para el tipo de hallazgo
  const getHallazgoTypeLabel = (tipo) => {
    const tipos = {
      'NC': { label: 'No Conformidad', clase: 'hallazgo-nc' },
      'OBS': { label: 'Observación', clase: 'hallazgo-obs' },
      'OM': { label: 'Oportunidad de Mejora', clase: 'hallazgo-om' }
    };
    return tipos[tipo] || { label: tipo, clase: '' };
  };

  // Obtener etiqueta visual para resultado
  const getResultadoLabel = (resultado) => {
    const resultados = {
      'Conforme': 'resultado-conforme',
      'No Conforme': 'resultado-no-conforme',
      'No Evaluado': 'resultado-no-evaluado'
    };
    return resultados[resultado] || '';
  };

  return (
    <div className="auditoria-execution-screen">
      <header className="screen-header">
        <h1>Ejecución de Auditoría ISO 9001</h1>
        <p>Sistema de Validación de Cumplimiento con Tailoring Dinámico</p>
      </header>

      <main className="screen-content">
        {/* FASE 1: FORMULARIO */}
        {status === 'form' && (
          <section className="phase-form">
            <div className="form-container">
              <h2>Configuración de Auditoría</h2>
              
              <div className="form-group">
                <label htmlFor="proyecto">Proyecto:</label>
                <select
                  id="proyecto"
                  value={proyectoSeleccionado}
                  onChange={(e) => setProyectoSeleccionado(e.target.value)}
                  className="form-select"
                >
                  <option value="">-- Selecciona un proyecto --</option>
                  {proyectos.map((proy) => (
                    <option key={proy.id} value={proy.id}>
                      {proy.nombre}
                    </option>
                  ))}
                </select>
              </div>

              <div className="form-group">
                <label htmlFor="etapa">Etapa:</label>
                <select
                  id="etapa"
                  value={etapaSeleccionada}
                  onChange={(e) => setEtapaSeleccionada(e.target.value)}
                  className="form-select"
                >
                  <option value="">-- Selecciona una etapa --</option>
                  {etapas.map((etapa) => (
                    <option key={etapa.id} value={etapa.id}>
                      {etapa.nombre}
                    </option>
                  ))}
                </select>
              </div>

              {error && <div className="error-message">{error}</div>}

              <button
                onClick={handleEjecutarAuditoria}
                className="btn-primary"
                disabled={!proyectoSeleccionado || !etapaSeleccionada}
              >
                Ejecutar Auditoría
              </button>
            </div>
          </section>
        )}

        {/* FASE 2: CARGA */}
        {status === 'loading' && (
          <section className="phase-loading">
            <div className="loading-container">
              <div className="spinner"></div>
              <h2>Auditoría en curso...</h2>
              <p>Por favor espera mientras se ejecutan los análisis</p>
              <div className="progress-steps">
                <div className="step active">Análisis de Documentos</div>
                <div className="step">Validación de Cumplimiento</div>
                <div className="step">Generación de Reporte</div>
              </div>
            </div>
          </section>
        )}

        {/* FASE 3: RESULTADOS */}
        {status === 'results' && resultados && (
          <section className="phase-results">
            <div className="results-header">
              <h2>Resultados de la Auditoría</h2>
              <p>Proyecto: {proyectos.find(p => p.id === proyectoSeleccionado)?.nombre}</p>
              <p>Etapa: {etapas.find(e => e.id === etapaSeleccionada)?.nombre}</p>
            </div>

            {/* TABLA 1: ARTEFACTOS EVALUADOS */}
            <section className="results-section">
              <h3>Artefactos Evaluados</h3>
              <table className="results-table">
                <thead>
                  <tr>
                    <th>ID Artefacto</th>
                    <th>Nombre</th>
                    <th>Aplicabilidad Tailoring</th>
                    <th>Resultado</th>
                    <th>Justificación</th>
                  </tr>
                </thead>
                <tbody>
                  {resultados.artefactosEvaluados.map((artefacto) => (
                    <tr key={artefacto.artefactoEsperadoId} className="artefacto-row">
                      <td className="cell-id">{artefacto.artefactoEsperadoId}</td>
                      <td>{artefacto.nombreArtefacto}</td>
                      <td>
                        <span className={`badge estado-${artefacto.estadoAplicacionTailoring.toLowerCase()}`}>
                          {artefacto.estadoAplicacionTailoring}
                        </span>
                      </td>
                      <td>
                        <span className={`badge ${getResultadoLabel(artefacto.resultado)}`}>
                          {artefacto.resultado}
                        </span>
                      </td>
                      <td>{artefacto.justificacionNoAplica || '-'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>

            {/* TABLA 2: HALLAZGOS */}
            <section className="results-section">
              <h3>Hallazgos Detectados</h3>
              {resultados.hallazgos.length > 0 ? (
                <table className="results-table">
                  <thead>
                    <tr>
                      <th>ID</th>
                      <th>Artefacto</th>
                      <th>Tipo</th>
                      <th>Descripción</th>
                      <th>Agente Origen</th>
                      <th>Fecha</th>
                    </tr>
                  </thead>
                  <tbody>
                    {resultados.hallazgos.map((hallazgo) => {
                      const tipoInfo = getHallazgoTypeLabel(hallazgo.tipo);
                      return (
                        <tr key={hallazgo.id} className="hallazgo-row">
                          <td className="cell-id">{hallazgo.id}</td>
                          <td>{hallazgo.artefactoEsperadoId}</td>
                          <td>
                            <span className={`badge ${tipoInfo.clase}`}>
                              {tipoInfo.label}
                            </span>
                          </td>
                          <td className="cell-descripcion">{hallazgo.descripcion}</td>
                          <td>{hallazgo.agenteOrigen}</td>
                          <td>{hallazgo.fechaDeteccion}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              ) : (
                <p className="no-data">No se detectaron hallazgos</p>
              )}
            </section>

            {/* TABLA 3: DOCUMENTOS ANALIZADOS */}
            <section className="results-section">
              <h3>Documentos Analizados</h3>
              <table className="results-table">
                <thead>
                  <tr>
                    <th>Nombre Archivo</th>
                    <th>Fuente</th>
                    <th>Tamaño</th>
                    <th>Fecha Carga</th>
                    <th>Hash Contenido</th>
                  </tr>
                </thead>
                <tbody>
                  {resultados.documentosAnalizados.map((doc) => (
                    <tr key={doc.id} className="documento-row">
                      <td className="cell-filename">{doc.nombreArchivo}</td>
                      <td>{doc.fuente}</td>
                      <td>{doc.tamaño}</td>
                      <td>{doc.fechaCarga}</td>
                      <td className="cell-hash" title={doc.hashContenido}>
                        {doc.hashContenido.substring(0, 16)}...
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </section>

            {/* BOTÓN REINICIAR */}
            <div className="results-actions">
              <button onClick={handleReiniciar} className="btn-secondary">
                Ejecutar Nueva Auditoría
              </button>
            </div>
          </section>
        )}
      </main>

      <footer className="screen-footer">
        <p>&copy; 2026 Sistema de Auditoría ISO 9001 | Versión 1.0</p>
      </footer>
    </div>
  );
};

export default AuditoriaExecutionScreen;
