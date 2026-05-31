import { api } from './client';

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
