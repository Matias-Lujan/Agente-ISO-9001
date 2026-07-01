// ============================================================================
//  Pantalla de Configuracion.
//
//  Tabs:
//   1. Mi perfil       — datos del usuario logueado + info sobre contrasena
//   2. Notificaciones  — placeholders con badge "Funcion en desarrollo"
//   3. Integraciones   — estado de conexiones (mock visual)
//   4. Agente IA       — modelo actual y opciones (placeholders)
//
//  Sobre la contrasena:
//   El usuario cambia su propia contrasena con PUT /api/auth/me/password, que
//   exige la contrasena actual (la verifica el backend). El JWT identifica al
//   usuario via la cookie HttpOnly — no viaja ningun id en el body.
// ============================================================================

import { useEffect, useState, type FormEvent, type ReactNode } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { configuracionCss } from '../styles/configuracion';
import { useAuth } from '../login/AuthContext';
import { cambiarPassword } from '../login/authApi';
import { obtenerConfig, obtenerConsumoTokens, type ResumenConsumoTokens } from '../api/config';
import {
  obtenerPrompt,
  actualizarPrompt,
  restablecerPrompt,
  revertirPrompt,
  type PromptAgente,
} from '../api/prompts';

type Tab = 'perfil' | 'notif' | 'agente';

export default function Configuracion() {
  useInjectStyle(configuracionCss, 'configuracion-style');

  const { usuario, cambiarTema } = useAuth();
  const [tab, setTab] = useState<Tab>('perfil');

  const inicial = usuario?.nombre?.trim().charAt(0).toUpperCase() ?? '—';
  const esOscuro = usuario?.tema === 'oscuro';

  return (
    <>
      <div className="cfg-header">
        <div>
          <h1 className="cfg-header-title">Configuración</h1>
          <p className="cfg-header-sub">Preferencias de tu cuenta y del sistema de auditoría</p>
        </div>

        {/* Switch modo oscuro — preferencia por usuario, se persiste en la BD */}
        <button
          type="button"
          className="cfg-theme"
          role="switch"
          aria-checked={esOscuro}
          aria-label="Modo oscuro"
          onClick={() => cambiarTema(esOscuro ? 'claro' : 'oscuro')}
        >
          <span className="cfg-theme-label">Modo oscuro</span>
          <span className={`cfg-switch${esOscuro ? ' on' : ''}`} />
        </button>
      </div>

      <div className="cfg-tabs">
        <button
          type="button"
          className={`cfg-tab ${tab === 'perfil' ? 'active' : ''}`}
          onClick={() => setTab('perfil')}
        >
          Mi perfil
        </button>
        <button
          type="button"
          className={`cfg-tab ${tab === 'notif' ? 'active' : ''}`}
          onClick={() => setTab('notif')}
        >
          Notificaciones
        </button>
        <button
          type="button"
          className={`cfg-tab ${tab === 'agente' ? 'active' : ''}`}
          onClick={() => setTab('agente')}
        >
          Agente IA
        </button>
      </div>

      {tab === 'perfil' && (
        <PerfilPanel
          inicial={inicial}
          nombre={usuario?.nombre ?? ''}
          email={usuario?.email ?? ''}
          rol={usuario?.rol ?? ''}
        />
      )}

      {tab === 'notif' && <NotificacionesPanel />}
      {tab === 'agente' && <AgenteIaPanel />}
    </>
  );
}

// ============================================================================
//  PANEL: MI PERFIL
// ============================================================================

interface PerfilPanelProps {
  inicial: string;
  nombre: string;
  email: string;
  rol: string;
}

function PerfilPanel({ inicial, nombre, email, rol }: PerfilPanelProps) {
  return (
    <>
      {/* Card 1: Informacion personal (solo lectura, datos del JWT) */}
      <div className="cfg-card">
        <div className="cfg-card-title">Información personal</div>

        <div className="cfg-avatar-section">
          <div className="cfg-big-avatar">{inicial}</div>
          <div>
            <div className="cfg-avatar-name">{nombre || 'Usuario'}</div>
            <div className="cfg-role-badge">{rol || 'Sin rol'}</div>
          </div>
        </div>

        <div className="cfg-field">
          <label>Nombre completo</label>
          <input className="cfg-input readonly" value={nombre} disabled readOnly />
        </div>

        <div className="cfg-field">
          <label>Correo electrónico</label>
          <input className="cfg-input readonly" value={email} disabled readOnly />
          <div className="cfg-field-hint">
            Tu correo es tu usuario de acceso. Contactá al administrador para cambiarlo.
          </div>
        </div>

        <div className="cfg-field">
          <label>Rol en el sistema</label>
          <input className="cfg-input readonly" value={rol} disabled readOnly />
        </div>
      </div>

      {/* Card 2: Cambio de contrasena (formulario real) */}
      <PasswordChangeCard />
    </>
  );
}

// ── Card de cambio de contrasena ────────────────────────────────────────────
//
// El usuario cambia su propia contrasena. Pide la actual (la verifica el
// backend) + la nueva x2. Nunca mostramos la contrasena guardada: solo existe
// su hash. Los errores del backend (ej: "La contraseña actual es incorrecta")
// llegan via el `mensaje` que propaga api/client.ts.
function PasswordChangeCard() {
  const [actual, setActual]     = useState('');
  const [nueva, setNueva]       = useState('');
  const [confirmar, setConfirmar] = useState('');
  const [ver, setVer]           = useState(false);
  const [enviando, setEnviando] = useState(false);
  const [error, setError]       = useState('');
  const [exito, setExito]       = useState(false);

  const handleSubmit = async (e: FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    setError('');
    setExito(false);

    if (nueva.length < 8) {
      setError('La nueva contraseña debe tener al menos 8 caracteres.');
      return;
    }
    if (nueva !== confirmar) {
      setError('La nueva contraseña y su confirmación no coinciden.');
      return;
    }
    if (nueva === actual) {
      setError('La nueva contraseña debe ser distinta a la actual.');
      return;
    }

    setEnviando(true);
    try {
      await cambiarPassword({ passwordActual: actual, passwordNueva: nueva });
      setExito(true);
      setActual('');
      setNueva('');
      setConfirmar('');
    } catch (err) {
      const msg = err instanceof Error ? err.message : 'No se pudo cambiar la contraseña.';
      // Limpia el prefijo "400 Bad Request — " para una UX mas linda.
      setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
    } finally {
      setEnviando(false);
    }
  };

  return (
    <div className="cfg-card">
      <div className="cfg-card-title">Cambiar contraseña</div>
      <div className="cfg-card-sub">Ingresá tu contraseña actual y elegí una nueva</div>

      <form onSubmit={handleSubmit} noValidate>
        {error && <div className="cfg-feedback error">{error}</div>}
        {exito && <div className="cfg-feedback success">Contraseña actualizada correctamente.</div>}

        <div className="cfg-field">
          <label htmlFor="cfg-pass-actual">Contraseña actual</label>
          <input
            id="cfg-pass-actual"
            className="cfg-input"
            type={ver ? 'text' : 'password'}
            value={actual}
            onChange={(e) => setActual(e.target.value)}
            disabled={enviando}
            autoComplete="current-password"
          />
        </div>

        <div className="cfg-field">
          <label htmlFor="cfg-pass-nueva">Nueva contraseña</label>
          <input
            id="cfg-pass-nueva"
            className="cfg-input"
            type={ver ? 'text' : 'password'}
            value={nueva}
            onChange={(e) => setNueva(e.target.value)}
            placeholder="Mínimo 8 caracteres"
            disabled={enviando}
            autoComplete="new-password"
          />
        </div>

        <div className="cfg-field">
          <label htmlFor="cfg-pass-confirmar">Confirmar nueva contraseña</label>
          <input
            id="cfg-pass-confirmar"
            className="cfg-input"
            type={ver ? 'text' : 'password'}
            value={confirmar}
            onChange={(e) => setConfirmar(e.target.value)}
            placeholder="Repetí la nueva contraseña"
            disabled={enviando}
            autoComplete="new-password"
          />
        </div>

        <label className="cfg-pass-show">
          <input
            type="checkbox"
            checked={ver}
            onChange={(e) => setVer(e.target.checked)}
          />
          Mostrar contraseñas
        </label>

        <div className="cfg-form-actions">
          <button type="submit" className="cfg-btn" disabled={enviando}>
            {enviando ? 'Guardando…' : 'Cambiar contraseña'}
          </button>
        </div>
      </form>
    </div>
  );
}

// ============================================================================
//  PANEL: NOTIFICACIONES
// ============================================================================

const NOTIF_EMAIL = [
  { name: 'Auditoría completada',
    desc: 'Recibís un email cuando una auditoría termina de ejecutarse' },
  { name: 'Nueva no conformidad detectada',
    desc: 'Te avisamos cada vez que se detecta una NC en tus proyectos' },
  { name: 'Informe generado',
    desc: 'Recibís el informe como adjunto cuando se genera automáticamente' },
  { name: 'Resumen semanal',
    desc: 'Un resumen de actividad todos los lunes a las 9am' },
];

const NOTIF_SISTEMA = [
  { name: 'Error en integración',
    desc: 'Te alertamos si falla la conexión con Drive, Trello o Clockify' },
  { name: 'Sonido de notificación',
    desc: 'Reproducir un sonido cuando llegue una alerta' },
];

function NotificacionesPanel() {
  return (
    <>
      <RoadmapNote>
        Las notificaciones están planificadas para una próxima versión. Abajo
        podés ver las alertas que vas a poder activar.
      </RoadmapNote>

      <div className="cfg-card">
        <div className="cfg-card-title">Notificaciones por email</div>
        <div className="cfg-card-sub">Elegí cuándo querés recibir alertas en tu correo</div>
        {NOTIF_EMAIL.map((item) => (
          <ToggleDevRow key={item.name} name={item.name} desc={item.desc} />
        ))}
      </div>

      <div className="cfg-card">
        <div className="cfg-card-title">Notificaciones del sistema</div>
        {NOTIF_SISTEMA.map((item) => (
          <ToggleDevRow key={item.name} name={item.name} desc={item.desc} />
        ))}
      </div>
    </>
  );
}

// Aviso honesto: la sección muestra funciones planificadas, todavía no activas.
function RoadmapNote({ children }: { children: ReactNode }) {
  return (
    <div className="cfg-roadmap-note">
      <span className="cfg-roadmap-tag">Próximas funciones</span>
      <span className="cfg-roadmap-text">{children}</span>
    </div>
  );
}

function ToggleDevRow({ name, desc }: { name: string; desc: string }) {
  return (
    <div className="cfg-toggle-row">
      <div className="cfg-toggle-info">
        <div className="cfg-toggle-name">{name}</div>
        <div className="cfg-toggle-desc">{desc}</div>
      </div>
      <span className="cfg-dev-badge">Función en desarrollo</span>
    </div>
  );
}

// ============================================================================
//  PANEL: AGENTE IA
// ============================================================================

const OPCIONES_CLASIFICACION = [
  { name: 'Incluir oportunidades de mejora',
    desc: 'El agente también detecta OM además de NC y Observaciones' },
  { name: 'Mostrar cláusula ISO en hallazgos',
    desc: 'El agente indica qué cláusula ISO 9001 aplica a cada hallazgo' },
  { name: 'Generar informe automáticamente',
    desc: 'Al terminar la auditoría, el agente genera el informe sin intervención' },
  { name: 'Modo estricto ISO 9001',
    desc: 'El agente aplica criterios más estrictos basados en la versión 2015 de la norma' },
];

function AgenteIaPanel() {
  const { usuario } = useAuth();
  const esAdmin = usuario?.rol === 'Administrador';

  // El modelo de IA lo define el administrador en la config del sistema. Lo
  // traemos del backend (GET /api/config) en vez de hardcodearlo, para que la
  // pantalla muestre siempre el valor real.
  const [modeloIa, setModeloIa] = useState('');

  useEffect(() => {
    let activo = true;
    obtenerConfig()
      .then((c) => { if (activo) setModeloIa(c.modeloIa); })
      .catch(() => { if (activo) setModeloIa(''); });
    return () => { activo = false; };
  }, []);

  return (
    <>
      <div className="cfg-card">
        <div className="cfg-card-title">Comportamiento del agente</div>
        <div className="cfg-card-sub">Configurá cómo el agente IA clasifica y reporta hallazgos</div>

        <div className="cfg-field">
          <label>Modelo de IA actual</label>
          <div className="cfg-model-current">
            <div className="cfg-model-info">
              <div className="cfg-model-icon">✦</div>
              <div>
                <div className="cfg-model-name">{modeloIa || 'Cargando…'}</div>
                <div className="cfg-model-meta">Definido por el administrador del sistema</div>
              </div>
            </div>
          </div>
          <div className="cfg-field-hint">
            El modelo se configura a nivel sistema. Contactá al administrador para cambiarlo.
          </div>
        </div>
      </div>

      {/* KPI de consumo de tokens — solo Administrador (endpoint admin-only).
          Es información de costo/uso del sistema. */}
      {esAdmin && <ConsumoTokensCard />}

      {/* Editor de system prompt — solo Administrador (los endpoints también
          son admin-only). Los demás roles no ven esta card. */}
      {esAdmin && <PromptEditorCard />}

      <RoadmapNote>
        Estas opciones de clasificación están planificadas. Hoy el agente aplica
        los criterios ISO 9001 definidos por el sistema.
      </RoadmapNote>

      <div className="cfg-card">
        <div className="cfg-card-title">Opciones de clasificación</div>
        {OPCIONES_CLASIFICACION.map((item) => (
          <ToggleDevRow key={item.name} name={item.name} desc={item.desc} />
        ))}
      </div>
    </>
  );
}

// ── KPI de consumo de tokens (admin-only) ───────────────────────────────────
//
// Muestra cuánto consume el agente IA de la app: el total (entrada/salida/total,
// cantidad de llamadas y de auditorías) y el desglose por agente. Los datos los
// acumula el backend por cada llamada al LLM y se agregan en
// GET /api/config/consumo-tokens. Sugerencia de cátedra: dar visibilidad al costo.

// Nombres legibles para las keys internas de los agentes.
const NOMBRE_AGENTE: Record<string, string> = {
  DocumentAnalysis: 'Analizador documental',
  ComplianceValidation: 'Validación de cumplimiento',
  ConsistencyVerification: 'Verificación de consistencia',
  FindingsClassification: 'Clasificación de hallazgos',
};

// Orden fijo del pipeline para mostrar la tabla: primero el análisis documental,
// después las validaciones, y al final la clasificación de hallazgos. Los agentes
// no listados quedan al final.
const ORDEN_AGENTE = [
  'DocumentAnalysis',
  'ComplianceValidation',
  'ConsistencyVerification',
  'FindingsClassification',
];

function ordenAgente(key: string): number {
  const i = ORDEN_AGENTE.indexOf(key);
  return i === -1 ? ORDEN_AGENTE.length : i;
}

function nombreAgente(key: string): string {
  return NOMBRE_AGENTE[key] ?? key;
}

function formatearNumero(n: number): string {
  return n.toLocaleString('es-AR');
}

function ConsumoTokensCard() {
  const [datos, setDatos]     = useState<ResumenConsumoTokens | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError]     = useState('');

  useEffect(() => {
    let activo = true;
    obtenerConsumoTokens()
      .then((d) => { if (activo) setDatos(d); })
      .catch((e) => { if (activo) setError(mensajeError(e)); })
      .finally(() => { if (activo) setCargando(false); });
    return () => { activo = false; };
  }, []);

  if (cargando) {
    return (
      <div className="cfg-card">
        <div className="cfg-card-title">Consumo de tokens</div>
        <div className="cfg-field-hint">Cargando…</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="cfg-card">
        <div className="cfg-card-title">Consumo de tokens</div>
        <div className="cfg-feedback error">{error}</div>
      </div>
    );
  }

  const total = datos?.total;
  const sinDatos = !total || total.cantidadLlamadas === 0;

  return (
    <div className="cfg-card">
      <div className="cfg-card-title">Consumo de tokens del agente IA</div>
      <div className="cfg-card-sub">
        Tokens consumidos por las auditorías desde que se activó la medición
      </div>

      {sinDatos ? (
        <div className="cfg-field-hint">
          Todavía no hay consumo registrado. Ejecutá una auditoría para ver el KPI.
        </div>
      ) : (
        <>
          <div className="cfg-kpi-grid">
            <div className="cfg-kpi">
              <div className="cfg-kpi-value">{formatearNumero(total!.tokensTotal)}</div>
              <div className="cfg-kpi-label">Tokens totales</div>
            </div>
            <div className="cfg-kpi">
              <div className="cfg-kpi-value">{formatearNumero(total!.tokensEntrada)}</div>
              <div className="cfg-kpi-label">Tokens de entrada</div>
            </div>
            <div className="cfg-kpi">
              <div className="cfg-kpi-value">{formatearNumero(total!.tokensSalida)}</div>
              <div className="cfg-kpi-label">Tokens de salida</div>
            </div>
            <div className="cfg-kpi">
              <div className="cfg-kpi-value">{formatearNumero(total!.cantidadLlamadas)}</div>
              <div className="cfg-kpi-label">Llamadas al LLM</div>
            </div>
            <div className="cfg-kpi">
              <div className="cfg-kpi-value">{formatearNumero(total!.cantidadAuditorias)}</div>
              <div className="cfg-kpi-label">Auditorías</div>
            </div>
          </div>

          <div className="cfg-consumo-tabla">
            <div className="cfg-consumo-row cfg-consumo-head">
              <span className="cfg-consumo-agente">Agente</span>
              <span>Entrada</span>
              <span>Salida</span>
              <span>Total</span>
              <span>Llamadas</span>
            </div>
            {[...datos!.porAgente]
              .sort((a, b) => ordenAgente(a.agenteKey) - ordenAgente(b.agenteKey))
              .map((a) => (
              <div key={a.agenteKey} className="cfg-consumo-row">
                <span className="cfg-consumo-agente">{nombreAgente(a.agenteKey)}</span>
                <span>{formatearNumero(a.tokensEntrada)}</span>
                <span>{formatearNumero(a.tokensSalida)}</span>
                <span>{formatearNumero(a.tokensTotal)}</span>
                <span>{formatearNumero(a.cantidadLlamadas)}</span>
              </div>
            ))}
          </div>
        </>
      )}
    </div>
  );
}

// ── Editor del system prompt de DocumentAnalysis (admin-only) ───────────────
//
// DocumentAnalysis es el único agente cuyo comportamiento vive en el system
// prompt (los otros tres lo tienen en su prompt de turno). El backend versiona
// cada cambio: guardar crea una versión nueva y activa; el historial permite
// revertir. Por eso el editor incluye un aviso fuerte: un prompt mal editado
// rompe el pipeline, pero "Restablecer" y el historial son la red de seguridad.
const AGENTE_KEY = 'DocumentAnalysis';

function PromptEditorCard() {
  const [prompt, setPrompt]       = useState<PromptAgente | null>(null);
  const [texto, setTexto]         = useState('');
  const [comentario, setComentario] = useState('');
  const [cargando, setCargando]   = useState(true);
  const [guardando, setGuardando] = useState(false);
  const [error, setError]         = useState('');
  const [ok, setOk]               = useState('');
  const [verHistorial, setVerHistorial] = useState(false);

  const aplicar = (p: PromptAgente) => {
    setPrompt(p);
    setTexto(p.contenido);
  };

  useEffect(() => {
    let activo = true;
    obtenerPrompt(AGENTE_KEY)
      .then((p) => { if (activo) aplicar(p); })
      .catch((e) => { if (activo) setError(mensajeError(e)); })
      .finally(() => { if (activo) setCargando(false); });
    return () => { activo = false; };
  }, []);

  const modificado = prompt !== null && texto !== prompt.contenido;

  const correr = async (accion: () => Promise<PromptAgente>, comentarioReset = true) => {
    setGuardando(true);
    setError('');
    setOk('');
    try {
      const p = await accion();
      aplicar(p);
      if (comentarioReset) setComentario('');
      setOk('Cambios aplicados. Las próximas auditorías usarán este prompt.');
    } catch (e) {
      setError(mensajeError(e));
    } finally {
      setGuardando(false);
    }
  };

  if (cargando) {
    return (
      <div className="cfg-card">
        <div className="cfg-card-title">System prompt del agente</div>
        <div className="cfg-field-hint">Cargando…</div>
      </div>
    );
  }

  return (
    <div className="cfg-card">
      <div className="cfg-prompt-head">
        <div>
          <div className="cfg-card-title" style={{ marginBottom: 2 }}>
            System prompt — Analizador Documental
          </div>
          <div className="cfg-card-sub" style={{ margin: 0 }}>
            Instrucciones base del agente DocumentAnalysis · versión {prompt?.versionActiva ?? '—'}
          </div>
        </div>
        <span className={`cfg-prompt-tag ${prompt?.esDefault ? 'is-default' : 'is-mod'}`}>
          {prompt?.esDefault ? 'Por defecto' : 'Modificado'}
        </span>
      </div>

      <div className="cfg-prompt-warn">
        <strong>⚠ Cuidado:</strong> este prompt controla el formato JSON que el
        sistema procesa. Un cambio incorrecto puede hacer fallar las auditorías.
        Ante la duda, usá <em>Restablecer al valor por defecto</em>.
      </div>

      {error && <div className="cfg-feedback error">{error}</div>}
      {ok && <div className="cfg-feedback success">{ok}</div>}

      <textarea
        className="cfg-prompt-textarea"
        value={texto}
        onChange={(e) => setTexto(e.target.value)}
        spellCheck={false}
        disabled={guardando}
      />

      <div className="cfg-field" style={{ marginTop: 12 }}>
        <label htmlFor="cfg-prompt-comentario">Comentario del cambio (opcional)</label>
        <input
          id="cfg-prompt-comentario"
          className="cfg-input"
          value={comentario}
          onChange={(e) => setComentario(e.target.value)}
          placeholder="Ej: ajuste en las reglas de matcheo por código"
          disabled={guardando}
        />
      </div>

      <div className="cfg-prompt-actions">
        <button
          type="button"
          className="cfg-btn-sec"
          onClick={() => correr(() => restablecerPrompt(AGENTE_KEY))}
          disabled={guardando}
        >
          Restablecer al valor por defecto
        </button>
        <div style={{ flex: 1 }} />
        <button
          type="button"
          className="cfg-btn-sec"
          onClick={() => setVerHistorial((v) => !v)}
          disabled={guardando}
        >
          {verHistorial ? 'Ocultar historial' : `Historial (${prompt?.historial.length ?? 0})`}
        </button>
        <button
          type="button"
          className="cfg-btn"
          onClick={() => correr(() => actualizarPrompt(AGENTE_KEY, texto, comentario.trim() || null))}
          disabled={guardando || !modificado}
        >
          {guardando ? 'Guardando…' : 'Guardar nueva versión'}
        </button>
      </div>

      {verHistorial && prompt && (
        <div className="cfg-prompt-hist">
          {prompt.historial.map((v) => (
            <div key={v.version} className="cfg-prompt-hist-row">
              <div className="cfg-prompt-hist-info">
                <span className="cfg-prompt-hist-ver">
                  v{v.version}{v.esActiva ? ' · activa' : ''}
                </span>
                <span className="cfg-prompt-hist-meta">
                  {formatearFecha(v.fechaCreacion)}
                  {v.modificadoPorNombre ? ` · ${v.modificadoPorNombre}` : ' · sistema'}
                  {v.comentario ? ` · ${v.comentario}` : ''}
                </span>
              </div>
              {!v.esActiva && (
                <button
                  type="button"
                  className="cfg-btn-sec sm"
                  onClick={() => correr(() => revertirPrompt(AGENTE_KEY, v.version), false)}
                  disabled={guardando}
                >
                  Revertir
                </button>
              )}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}

function mensajeError(e: unknown): string {
  const msg = e instanceof Error ? e.message : 'Ocurrió un error.';
  return msg.replace(/^\d+\s+\w+\s+—\s+/, '');
}

function formatearFecha(iso: string): string {
  const d = new Date(iso);
  return Number.isNaN(d.getTime()) ? iso : d.toLocaleString('es-AR');
}