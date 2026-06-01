import { api } from './client';

export interface Proyecto {
  id: number;
  nombre: string;
  procedimientoId: number;
}

export function listarProyectos(): Promise<Proyecto[]> {
  return api.get<Proyecto[]>('/api/proyectos');
}
