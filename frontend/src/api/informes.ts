// ============================================================================
//  Endpoints de informes de auditoría.
//
//  Un informe existe solo cuando la auditoría YA se generó: por eso esta
//  pantalla lista informes ya generados, listos para ver / descargar.
//
//  Visibilidad por rol:
//   - Administrador / Auditor → GET /api/informes (todos)
//   - Operador                → no puede listar todos; se arman a partir de sus
//     proyectos asignados (auditorías → informes), usando endpoints permitidos
//     para cualquier autenticado.
// ============================================================================
import { api } from './client';

// Espejo de InformeResponse del backend.
export interface Informe {
  id: number;
  auditoriaId: number;
  nombreProyecto: string;
  fechaGeneracion: string;
  tipo: string;        // enum TipoInforme: "Auto" | "Manual"
  contenido: string;
}

// Etiquetas legibles para el tipo de informe.
export const TIPO_INFORME_LABEL: Record<string, string> = {
  Auto: 'Automático',
  Manual: 'Manual',
};

// Todos los informes (solo Administrador / Auditor según el backend).
export function listarInformes(): Promise<Informe[]> {
  return api.get<Informe[]>('/api/informes');
}

// Informes de una auditoría puntual (cualquier autenticado).
export function listarInformesDeAuditoria(auditoriaId: number): Promise<Informe[]> {
  return api.get<Informe[]>(`/api/informes/auditoria/${auditoriaId}`);
}

export function obtenerInforme(id: number): Promise<Informe> {
  return api.get<Informe>(`/api/informes/${id}`);
}