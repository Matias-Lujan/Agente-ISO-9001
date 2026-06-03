// ============================================================================
//  Módulo de hallazgos — datos reales del backend.
//
//  No existe un endpoint "listar todos los hallazgos", así que esta capa los
//  arma combinando los endpoints reales que SÍ existen (igual que el dashboard):
//    1. GET /api/proyectos                         → proyectos visibles
//    2. GET /api/auditorias/proyecto/{proyectoId}  → auditorías de cada proyecto
//    3. GET /api/hallazgos/auditoria/{auditoriaId} → hallazgos de cada auditoría
//
//  La pantalla (screens/Hallazgos.tsx) solo conoce `listarHallazgos()` y el
//  tipo `Hallazgo`: no sabe cómo se arma la lista.
//
//  Mapeo backend → frontend (el backend no expone todos los campos del MVP):
//   - titulo        ← nombreArtefacto
//   - descripcion   ← descripcion
//   - evidencia     ← justificacion (la justificación que dio el agente)
//   - proyecto      ← nombre del proyecto de la auditoría
//   - tipo          ← NC/OBS/OM → NoConformidad/Observacion/OportunidadMejora
//   - fechaDeteccion← fecha de inicio de la auditoría que lo originó
//   - estado        ← "Abierto" fijo: el modelo actual no tiene workflow de
//                     resolución, así que ningún hallazgo está resuelto todavía.
// ============================================================================

import { api } from './client';
import { listarProyectos } from './proyectos';

export type TipoHallazgo = 'NoConformidad' | 'Observacion' | 'OportunidadMejora';

export type EstadoHallazgo = 'Abierto' | 'EnRevision' | 'Resuelto';

export interface Hallazgo {
  id: number;
  titulo: string;
  descripcion: string;
  evidencia: string;
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

// ── Shapes tal cual los devuelve el backend ──────────────────────────────────

type TipoBackend = 'NC' | 'OBS' | 'OM';

// Espejo de HallazgoResponse (backend DTOs/HallazgoDTOs.cs)
interface HallazgoApi {
  id: number;
  auditoriaId: number;
  artefactoEvaluadoId: number;
  nombreArtefacto: string;
  tipo: TipoBackend;
  descripcion: string;
  justificacion: string;
  agenteOrigen: string;
}

// Espejo (parcial) de AuditoriaResponse (backend DTOs/AuditoriaDTOs.cs)
interface AuditoriaApi {
  id: number;
  proyectoId: number;
  nombreProyecto: string;
  fechaInicioUtc: string;
}

const TIPO_MAP: Record<TipoBackend, TipoHallazgo> = {
  NC:  'NoConformidad',
  OBS: 'Observacion',
  OM:  'OportunidadMejora',
};

function fechaMs(iso: string): number {
  const ms = Date.parse(iso);
  return Number.isNaN(ms) ? 0 : ms;
}

/**
 * Devuelve todos los hallazgos del sistema (datos reales), ordenados del más
 * reciente al más antiguo (por fecha de la auditoría que los originó).
 * Resiliente: si un proyecto no tiene auditorías o una auditoría no tiene
 * hallazgos, se tratan como vacíos en vez de romper toda la carga.
 */
export async function listarHallazgos(): Promise<Hallazgo[]> {
  const proyectos = await listarProyectos();

  // Auditorías de cada proyecto, en paralelo.
  const auditoriasAnidadas = await Promise.all(
    proyectos.map((p) =>
      api.get<AuditoriaApi[]>(`/api/auditorias/proyecto/${p.id}`).catch(() => []),
    ),
  );
  const auditorias = auditoriasAnidadas.flat();

  // Hallazgos de cada auditoría, en paralelo.
  const hallazgosAnidados = await Promise.all(
    auditorias.map((a) =>
      api.get<HallazgoApi[]>(`/api/hallazgos/auditoria/${a.id}`).catch(() => []),
    ),
  );

  // Índice auditoríaId → auditoría, para resolver proyecto y fecha del hallazgo.
  const auditoriaPorId = new Map<number, AuditoriaApi>();
  for (const a of auditorias) auditoriaPorId.set(a.id, a);

  return hallazgosAnidados
    .flat()
    .map((h): Hallazgo => {
      const auditoria = auditoriaPorId.get(h.auditoriaId);
      return {
        id: h.id,
        titulo: h.nombreArtefacto,
        descripcion: h.descripcion,
        evidencia: h.justificacion ?? '',
        proyecto: auditoria?.nombreProyecto ?? '—',
        tipo: TIPO_MAP[h.tipo] ?? 'Observacion',
        estado: 'Abierto',
        fechaDeteccion: auditoria?.fechaInicioUtc ?? '',
        agenteOrigen: h.agenteOrigen,
      };
    })
    .sort((a, b) => fechaMs(b.fechaDeteccion) - fechaMs(a.fechaDeteccion) || b.id - a.id);
}
