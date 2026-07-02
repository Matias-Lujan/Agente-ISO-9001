// ============================================================================
//  Configuracion del sistema (solo lectura).
//  El backend expone GET /api/config con valores informativos definidos por el
//  administrador en appsettings (ej: el modelo de IA). Nunca devuelve secretos.
// ============================================================================

import { api } from './client';

export interface ConfigSistema {
  modeloIa: string;
}

export function obtenerConfig(): Promise<ConfigSistema> {
  return api.get<ConfigSistema>('/api/config');
}

// ── Consumo de tokens (KPI, solo Administrador) ─────────────────────────────
// GET /api/config/consumo-tokens. Devuelve el total consumido por la app y el
// desglose por agente. Endpoint admin-only: para roles no-admin responde 403.

export interface ConsumoTotal {
  tokensEntrada: number;
  tokensSalida: number;
  tokensTotal: number;
  cantidadLlamadas: number;
  cantidadAuditorias: number;
}

export interface ConsumoPorAgente {
  agenteKey: string;
  tokensEntrada: number;
  tokensSalida: number;
  tokensTotal: number;
  cantidadLlamadas: number;
}

export interface ResumenConsumoTokens {
  total: ConsumoTotal;
  porAgente: ConsumoPorAgente[];
}

export function obtenerConsumoTokens(): Promise<ResumenConsumoTokens> {
  return api.get<ResumenConsumoTokens>('/api/config/consumo-tokens');
}
