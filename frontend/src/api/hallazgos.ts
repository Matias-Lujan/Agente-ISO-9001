
// ============================================================================
//  Modulo de hallazgos.
//  Por ahora trabaja con MOCK DATA local. Cuando el backend exponga
//  GET /api/hallazgos, reemplazamos listarHallazgos() por una llamada real
//  a `api.get<Hallazgo[]>('/api/hallazgos')` y el resto de la pantalla
//  sigue funcionando sin cambios.
// ============================================================================

export type TipoHallazgo = 'NoConformidad' | 'Observacion' | 'OportunidadMejora';

export type EstadoHallazgo = 'Abierto' | 'EnRevision' | 'Resuelto';

export interface Hallazgo {
  id: number;
  titulo: string;
  descripcion: string;
  evidencia: string;          // ej: "Trello: pendiente al 09/04 · 0 hs en Clockify"
  proyecto: string;
  tipo: TipoHallazgo;
  estado: EstadoHallazgo;
  fechaDeteccion: string;     // ISO date
  agenteOrigen?: string;
  justificacion?: string;
}

// ── Helpers de presentacion (label + color) ──────────────────────────────────

export const TIPO_LABEL: Record<TipoHallazgo, string> = {
  NoConformidad:      'NC',
  Observacion:        'OBS',
  OportunidadMejora:  'OM',
};

export const TIPO_LABEL_LARGO: Record<TipoHallazgo, string> = {
  NoConformidad:      'No conformidad',
  Observacion:        'Observación',
  OportunidadMejora:  'Oportunidad de mejora',
};

export const ESTADO_LABEL: Record<EstadoHallazgo, string> = {
  Abierto:    'Abierto',
  EnRevision: 'En revisión',
  Resuelto:   'Resuelto',
};

// ── Mock data ────────────────────────────────────────────────────────────────
// Replicamos los hallazgos del prototipo + algunos extras para que la
// paginacion tenga 3 paginas (28 hallazgos en total como en la captura).

const MOCK: Hallazgo[] = [
  {
    id: 1,
    titulo: 'Tarea 2 vencida sin justificación',
    descripcion: 'Tarea marcada como vencida en Trello sin nota explicativa ni horas imputadas en Clockify.',
    evidencia: 'Trello: pendiente al 09/04 · 0 hs en Clockify',
    proyecto: 'Facturación',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-04-09T10:00:00Z',
    agenteOrigen: 'ComplianceValidation',
  },
  {
    id: 2,
    titulo: 'Inconsistencia en responsable asignado',
    descripcion: 'El responsable definido en el contrato no coincide con el asignado en Trello.',
    evidencia: 'Contrato: Juan García · Trello: María López',
    proyecto: 'Facturación',
    tipo: 'NoConformidad',
    estado: 'EnRevision',
    fechaDeteccion: '2026-04-12T14:30:00Z',
    agenteOrigen: 'ConsistencyVerification',
  },
  {
    id: 3,
    titulo: 'Falta registro de revisión de calidad',
    descripcion: 'No se encontró el documento de revisión de calidad esperado para esta etapa.',
    evidencia: 'Drive: documento no encontrado en carpeta del proyecto',
    proyecto: 'Portal RR.HH.',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-04-15T09:15:00Z',
    agenteOrigen: 'DocumentAnalysis',
  },
  {
    id: 4,
    titulo: 'Documentación sin versionar',
    descripcion: 'Archivos en Drive no tienen número de versión ni fecha de aprobación visible.',
    evidencia: 'Drive: 3 archivos sin número de versión ni fecha de aprobación',
    proyecto: 'Portal RR.HH.',
    tipo: 'Observacion',
    estado: 'EnRevision',
    fechaDeteccion: '2026-04-18T11:45:00Z',
    agenteOrigen: 'DocumentAnalysis',
  },
  {
    id: 5,
    titulo: 'Horas no registradas en Clockify',
    descripcion: 'Se detectaron tareas finalizadas en Trello sin imputación de horas en Clockify.',
    evidencia: 'Trello: 5 tareas completadas · Clockify: 0 hs registradas',
    proyecto: 'ERP Logística',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-04-20T16:20:00Z',
    agenteOrigen: 'ConsistencyVerification',
  },
  {
    id: 6,
    titulo: 'Sincronizar responsables automáticamente entre Drive y Trello',
    descripcion: 'Patrón recurrente de desincronización entre tableros y documentos del proyecto.',
    evidencia: 'Detectado en 3 proyectos distintos',
    proyecto: 'Portal Proveedores',
    tipo: 'OportunidadMejora',
    estado: 'Resuelto',
    fechaDeteccion: '2026-04-22T08:00:00Z',
    agenteOrigen: 'FindingsClassification',
  },
  // ── Página 2 ────────────────────────────────────────────────────────────
  {
    id: 7,
    titulo: 'Procedimiento no aplicado completamente',
    descripcion: 'La etapa 3 del procedimiento PR-11-13 no presenta evidencias en el proyecto.',
    evidencia: 'Procedimiento: 5 entregables · Drive: 2 archivos',
    proyecto: 'CRM Comercial',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-04-25T13:00:00Z',
  },
  {
    id: 8,
    titulo: 'Acta de reunión faltante',
    descripcion: 'No se encontró el acta de la reunión de kickoff del proyecto.',
    evidencia: 'Drive: carpeta /actas vacía',
    proyecto: 'CRM Comercial',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-04-26T10:30:00Z',
  },
  {
    id: 9,
    titulo: 'Implementar validación automática de plantillas',
    descripcion: 'Se podrían validar las plantillas de documentos automáticamente al subirlas.',
    evidencia: 'Análisis de 12 proyectos similares',
    proyecto: 'Portal Cliente',
    tipo: 'OportunidadMejora',
    estado: 'EnRevision',
    fechaDeteccion: '2026-04-28T15:45:00Z',
  },
  {
    id: 10,
    titulo: 'Tarea sin asignar después de 7 días',
    descripcion: 'Tarea creada en Trello sin responsable asignado por más de una semana.',
    evidencia: 'Trello: creada 22/04 · sin asignar al 29/04',
    proyecto: 'ERP Logística',
    tipo: 'NoConformidad',
    estado: 'EnRevision',
    fechaDeteccion: '2026-04-29T09:00:00Z',
  },
  {
    id: 11,
    titulo: 'Documento aprobado sin firma del responsable',
    descripcion: 'Documento marcado como aprobado pero sin firma electrónica del responsable.',
    evidencia: 'Drive: 2 documentos sin firma',
    proyecto: 'Portal RR.HH.',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-02T11:00:00Z',
  },
  {
    id: 12,
    titulo: 'Mejorar nomenclatura de archivos',
    descripcion: 'Estandarizar el nombre de los archivos para facilitar búsqueda y trazabilidad.',
    evidencia: 'Recomendación de auditoría 2026-Q1',
    proyecto: 'Portal Proveedores',
    tipo: 'OportunidadMejora',
    estado: 'Resuelto',
    fechaDeteccion: '2026-05-05T14:00:00Z',
  },
  // ── Página 3 ────────────────────────────────────────────────────────────
  {
    id: 13,
    titulo: 'Diferencia de horas entre Trello y Clockify',
    descripcion: 'Las horas reportadas en Trello no coinciden con las registradas en Clockify.',
    evidencia: 'Trello: 40hs · Clockify: 32hs',
    proyecto: 'Facturación',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-08T10:00:00Z',
  },
  {
    id: 14,
    titulo: 'Revisión de calidad fuera de plazo',
    descripcion: 'La revisión de calidad se realizó 15 días después de lo establecido.',
    evidencia: 'Plazo: 10/04 · Ejecutada: 25/04',
    proyecto: 'CRM Comercial',
    tipo: 'Observacion',
    estado: 'EnRevision',
    fechaDeteccion: '2026-05-10T16:30:00Z',
  },
  {
    id: 15,
    titulo: 'Reporte mensual no entregado',
    descripcion: 'No se encontró el reporte mensual correspondiente a marzo.',
    evidencia: 'Drive: carpeta /reportes-marzo vacía',
    proyecto: 'Portal Cliente',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-12T09:15:00Z',
  },
  {
    id: 16,
    titulo: 'Falta de capacitación documentada',
    descripcion: 'No hay evidencia de capacitación en el procedimiento actualizado.',
    evidencia: 'RR.HH.: registros pendientes',
    proyecto: 'Portal RR.HH.',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-14T13:00:00Z',
  },
  {
    id: 17,
    titulo: 'Automatizar generación de informes',
    descripcion: 'Los informes podrían generarse automáticamente desde los datos del sistema.',
    evidencia: 'Tiempo manual estimado: 4hs/mes',
    proyecto: 'ERP Logística',
    tipo: 'OportunidadMejora',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-16T11:00:00Z',
  },
  {
    id: 18,
    titulo: 'Documento desactualizado en uso',
    descripcion: 'Se está utilizando una versión obsoleta del procedimiento PR-11.',
    evidencia: 'Versión vigente: v3.2 · En uso: v2.8',
    proyecto: 'CRM Comercial',
    tipo: 'NoConformidad',
    estado: 'EnRevision',
    fechaDeteccion: '2026-05-18T14:45:00Z',
  },
  {
    id: 19,
    titulo: 'Indicador KPI no medido',
    descripcion: 'El KPI de satisfacción del cliente no fue medido en el último trimestre.',
    evidencia: 'Dashboard: última medición 2026-01',
    proyecto: 'Portal Cliente',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-20T10:30:00Z',
  },
  {
    id: 20,
    titulo: 'Mejorar trazabilidad de cambios en documentos',
    descripcion: 'Implementar historial de cambios visible directamente en la plataforma.',
    evidencia: 'Solicitud recurrente de auditores',
    proyecto: 'Portal Proveedores',
    tipo: 'OportunidadMejora',
    estado: 'Resuelto',
    fechaDeteccion: '2026-05-22T15:00:00Z',
  },
  {
    id: 21,
    titulo: 'Acceso no revocado a ex-colaborador',
    descripcion: 'Se detectó un usuario inactivo con permisos vigentes en Drive.',
    evidencia: 'Auditoría de accesos 2026-05',
    proyecto: 'Portal RR.HH.',
    tipo: 'NoConformidad',
    estado: 'EnRevision',
    fechaDeteccion: '2026-05-23T09:00:00Z',
  },
  {
    id: 22,
    titulo: 'Faltan minutas de comité',
    descripcion: 'No se encontraron las minutas de los últimos 2 comités de calidad.',
    evidencia: 'Drive: 0 archivos en /comite-calidad',
    proyecto: 'Facturación',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-24T11:45:00Z',
  },
  {
    id: 23,
    titulo: 'Política de backup no documentada',
    descripcion: 'No existe documentación formal sobre la política de respaldos.',
    evidencia: 'Procedimientos: tema no cubierto',
    proyecto: 'ERP Logística',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-25T13:30:00Z',
  },
  {
    id: 24,
    titulo: 'Integrar Clockify con sistema de tickets',
    descripcion: 'Reducir la doble carga sincronizando ambas plataformas.',
    evidencia: 'Análisis de eficiencia',
    proyecto: 'CRM Comercial',
    tipo: 'OportunidadMejora',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-26T10:00:00Z',
  },
  {
    id: 25,
    titulo: 'Plan de mejora no implementado',
    descripcion: 'Acciones del plan de mejora 2026-Q1 sin avance registrado.',
    evidencia: '3 de 5 acciones sin estado',
    proyecto: 'Portal Cliente',
    tipo: 'NoConformidad',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-27T14:00:00Z',
  },
  {
    id: 26,
    titulo: 'Revisión de proveedores incompleta',
    descripcion: 'Evaluación anual de proveedores con 4 de 12 sin revisar.',
    evidencia: 'Registros de evaluación 2026',
    proyecto: 'Portal Proveedores',
    tipo: 'Observacion',
    estado: 'EnRevision',
    fechaDeteccion: '2026-05-28T16:00:00Z',
  },
  {
    id: 27,
    titulo: 'Indicador de tiempo de respuesta sin medir',
    descripcion: 'No se está midiendo el tiempo de respuesta a incidencias.',
    evidencia: 'KPIs definidos: 8 · Medidos: 6',
    proyecto: 'Portal RR.HH.',
    tipo: 'Observacion',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-29T09:30:00Z',
  },
  {
    id: 28,
    titulo: 'Estandarizar reportes de hallazgos',
    descripcion: 'Crear una plantilla única para todos los reportes de auditoría.',
    evidencia: 'Varia el formato entre auditores',
    proyecto: 'Facturación',
    tipo: 'OportunidadMejora',
    estado: 'Abierto',
    fechaDeteccion: '2026-05-30T11:00:00Z',
  },
];

// ── API "pública" (mock) ─────────────────────────────────────────────────────

/**
 * Devuelve todos los hallazgos del sistema.
 * Simula un pequeño delay de red.
 *
 * Cuando exista el endpoint real:
 *   return api.get<Hallazgo[]>('/api/hallazgos');
 */
export function listarHallazgos(): Promise<Hallazgo[]> {
  return new Promise((resolve) => {
    setTimeout(() => resolve(MOCK), 300);
  });
}