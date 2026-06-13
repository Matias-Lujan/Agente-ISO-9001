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

// ============================================================================
//  cargarRevision() — versión "anticipación al auditor".
//
//  En vez de un status puntual por hallazgo (que no aporta porque el usuario
//  corrige y re-ejecuta), esta función arma la foto de la ÚLTIMA ejecución
//  (auditoría Completada) de cada proyecto, derivada del resultado real:
//    GET /api/auditorias/proyecto/{id}  → última Completada
//    GET /api/auditorias/{auditoriaId}/resultado → artefactos evaluados
//
//  Devuelve, de forma consistente entre sí:
//   - hallazgos:  los detectados (problemas) de esas ejecuciones
//   - conformes:  los artefactos revisados que dieron OK (para no dejar la
//                 pantalla en blanco: deja constancia de que se revisó)
//   - contadores: revisados / conformes / no conformes
// ============================================================================
import { listarAuditoriasDeProyecto, obtenerResultado } from './auditorias';

export interface ItemConforme {
  id: number;
  codigo: string | null;
  nombre: string;
  proyecto: string;
}

export interface Revision {
  hallazgos: Hallazgo[];
  conformes: ItemConforme[];
  revisados: number;          // artefactos aplicables con veredicto
  totalConformes: number;
  totalNoConformes: number;
}

// El resultado puede serializar el tipo como 'NC'/'OBS'/'OM' o como el nombre
// del enum ('NoConformidad'/'Observacion'/'OportunidadMejora'): aceptamos ambos.
function mapTipoFlexible(t: string): TipoHallazgo {
  const x = (t ?? '').toLowerCase();
  if (x === 'nc' || x.includes('conformidad')) return 'NoConformidad';
  if (x === 'obs' || x.includes('observ')) return 'Observacion';
  return 'OportunidadMejora';
}

export async function cargarRevision(): Promise<Revision> {
  const proyectos = await listarProyectos();

  // Última auditoría Completada de cada proyecto (en paralelo).
  const auditoriasPorProyecto = await Promise.all(
    proyectos.map(async (p) => {
      const auds = await listarAuditoriasDeProyecto(p.id).catch(() => []);
      const completadas = auds
        .filter((a) => a.estado === 'Completada')
        .sort((a, b) => fechaMs(b.fechaInicioUtc) - fechaMs(a.fechaInicioUtc));
      return { proyecto: p, auditoria: completadas[0] ?? null };
    }),
  );

  const conResultado = await Promise.all(
    auditoriasPorProyecto.map(async ({ proyecto, auditoria }) => {
      if (!auditoria) return null;
      const res = await obtenerResultado(auditoria.id).catch(() => null);
      return res ? { proyecto, auditoria, res } : null;
    }),
  );

  const hallazgos: Hallazgo[] = [];
  const conformes: ItemConforme[] = [];
  let totalConformes = 0;
  let totalNoConformes = 0;

  for (const item of conResultado) {
    if (!item) continue;
    const { proyecto, auditoria, res } = item;

    for (const art of res.artefactosEvaluados) {
      if (art.aplica !== 'Aplica') continue;

      if (art.resultado === 'Conforme') {
        totalConformes += 1;
        conformes.push({
          id: art.id,
          codigo: art.artefactoEsperadoCodigo,
          nombre: art.artefactoEsperadoNombre ?? `Artefacto #${art.artefactoEsperadoId}`,
          proyecto: proyecto.nombre,
        });
      } else if (art.resultado === 'NoConforme') {
        totalNoConformes += 1;
      }

      for (const h of art.hallazgos) {
        hallazgos.push({
          id: h.id,
          titulo: art.artefactoEsperadoNombre ?? `Artefacto #${art.artefactoEsperadoId}`,
          descripcion: h.descripcion,
          evidencia: h.justificacion ?? '',
          proyecto: proyecto.nombre,
          tipo: mapTipoFlexible(h.tipo),
          estado: 'Abierto',
          fechaDeteccion: auditoria.fechaInicioUtc,
          agenteOrigen: h.agenteOrigen,
          justificacion: h.justificacion,
        });
      }
    }
  }

  hallazgos.sort(
    (a, b) => fechaMs(b.fechaDeteccion) - fechaMs(a.fechaDeteccion) || b.id - a.id,
  );

  return {
    hallazgos,
    conformes,
    revisados: totalConformes + totalNoConformes,
    totalConformes,
    totalNoConformes,
  };
}