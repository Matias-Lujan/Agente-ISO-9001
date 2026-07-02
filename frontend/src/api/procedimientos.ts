import { api } from './client';

export interface Procedimiento {
  id: number;
  codigo: string;
  nombre: string;
  descripcion: string | null;
}

export function listarProcedimientos(): Promise<Procedimiento[]> {
  return api.get<Procedimiento[]>('/api/procedimientos');
}

export interface Etapa {
  id: number;
  nombre: string;
  orden: number;
}

export function listarEtapasDeProcedimiento(
  procedimientoId: number
): Promise<Etapa[]> {
  return api.get<Etapa[]>(`/api/procedimientos/${procedimientoId}/etapas`);
}