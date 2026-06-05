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
//   No mostramos un formulario de cambio porque el backend todavia no expone
//   el endpoint /api/auth/cambiar-password. En lugar de un mock que enganaria
//   al usuario, mostramos una card informativa explicando como proceder.
//
//  Cuando exista el endpoint real:
//   - Crear src/api/auth-password.ts con la llamada a `api.post()`
//   - Reemplazar PasswordCard por un formulario con PasswordInput x3
//   - El JWT ya viaja en el header automaticamente
// ============================================================================

import { useState } from 'react';
import { useInjectStyle } from '../utils/useInjectStyle';
import { configuracionCss } from '../styles/configuracion';
import { useAuth } from '../login/AuthContext';

type Tab = 'perfil' | 'notif' | 'integ' | 'agente';

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
          className={`cfg-tab ${tab === 'integ' ? 'active' : ''}`}
          onClick={() => setTab('integ')}
        >
          Integraciones
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
      {tab === 'integ' && <IntegracionesPanel emailUsuario={usuario?.email ?? ''} />}
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

      {/* Card 2: Contrasena (informativa) */}
      <PasswordCard />
    </>
  );
}

// ── Card informativa de contrasena ─────────────────────────────────────────
//
// No mostramos la contrasena (es imposible: solo guardamos el hash) ni
// formulario de cambio (el endpoint no existe todavia). Le decimos al
// usuario que tiene que pedirle al administrador.
function PasswordCard() {
  return (
    <div className="cfg-card">
      <div className="cfg-card-title">Contraseña</div>

      <div className="cfg-pass-info">
        <div className="cfg-pass-info-icon" aria-hidden="true">
          <svg width="20" height="20" viewBox="0 0 24 24" fill="none"
            stroke="currentColor" strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round">
            <rect x="3" y="11" width="18" height="11" rx="2"/>
            <path d="M7 11V7a5 5 0 0110 0v4"/>
          </svg>
        </div>

        <div className="cfg-pass-info-text">
          <div className="cfg-pass-info-title">Tu contraseña está protegida</div>
          <div className="cfg-pass-info-desc">
            Por motivos de seguridad, las contraseñas no pueden visualizarse desde la plataforma.
            Si necesitás cambiarla, contactá al administrador del sistema.
          </div>
        </div>
      </div>
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
//  PANEL: INTEGRACIONES
// ============================================================================

function IntegracionesPanel({ emailUsuario }: { emailUsuario: string }) {
  const integraciones = [
    {
      name: 'Google Drive',
      sub:  `Cuenta: ${emailUsuario || 'no disponible'} · Último acceso: hace 2 min`,
      icon: '📁',
      iconBg: '#E8F0FE',
      status: 'ok' as const,
    },
    {
      name: 'Trello',
      sub:  'Workspace: BDT Global · Último acceso: hace 5 min',
      icon: '📋',
      iconBg: '#EBF5FB',
      status: 'ok' as const,
    },
    {
      name: 'Clockify',
      sub:  'Error de autenticación · Última conexión: 09/04/2026',
      icon: '⏱️',
      iconBg: '#FDEAEA',
      status: 'err' as const,
    },
  ];

  return (
    <div className="cfg-card">
        <div className="cfg-int-row" style={{ marginBottom: '15px' }}>
            <div className="cfg-int-icon" style={{ background: '#EDE8FC', color: '#4B2DAB' }}>
                <svg xmlns="http://www.w3.org/2000/svg" width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                <path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7Z"/>
                <circle cx="12" cy="12" r="3"/>
                </svg>
            </div>
            <div className="cfg-int-detail">
                <div className="cfg-int-detail-name">Vista de ejemplo</div>
                <div className="cfg-int-detail-sub">El monitoreo en vivo se incorporará próximamente.</div>
            </div>
        </div>
    
      <div className="cfg-card-title">Estado de conexiones</div>
      <div className="cfg-card-sub">Servicios externos conectados a tu cuenta</div>

      <div className="cfg-int-list">
        {integraciones.map((int) => (
          <div key={int.name} className="cfg-int-row">
            <div className="cfg-int-icon" style={{ background: int.iconBg }}>
              {int.icon}
            </div>
            <div className="cfg-int-detail">
              <div className="cfg-int-detail-name">{int.name}</div>
              <div className="cfg-int-detail-sub">{int.sub}</div>
            </div>
            <span className={`cfg-status-pill ${int.status === 'ok' ? 'cfg-status-ok' : 'cfg-status-err'}`}>
              {int.status === 'ok' ? '✓ Conectado' : '✕ Error'}
            </span>
          </div>
        ))}
      </div>
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
                <div className="cfg-model-name">Gemini 2.5 Flash</div>
                <div className="cfg-model-meta">Modelo recomendado · Rápido y económico</div>
              </div>
            </div>
            <span className="cfg-dev-badge">Configurable en desarrollo</span>
          </div>
          <div className="cfg-field-hint">
            El modelo actual es el configurado por el administrador del sistema.
          </div>
        </div>

        <div className="cfg-field">
          <label>Idioma de los informes generados</label>
          <input className="cfg-input readonly" value="Español" disabled readOnly />
        </div>

        <div className="cfg-field">
          <label>Instrucción personalizada al agente</label>
          <input
            className="cfg-input readonly"
            placeholder="Próximamente: podrás dar instrucciones específicas al agente"
            disabled
            readOnly
          />
          <div className="cfg-field-hint">
            Se agregará al system prompt del agente en cada auditoría
          </div>
        </div>
      </div>

      <div className="cfg-card">
        <div className="cfg-card-title">Opciones de clasificación</div>
        {OPCIONES_CLASIFICACION.map((item) => (
          <ToggleDevRow key={item.name} name={item.name} desc={item.desc} />
        ))}
      </div>
    </>
  );
}