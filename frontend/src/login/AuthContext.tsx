// ============================================================================
//  Contexto global de autenticacion.
//
//  El JWT NO se guarda en el cliente: vive en una cookie HttpOnly que setea el
//  backend en /api/auth/login. El JS no puede leerla (mitiga XSS) y el browser
//  la manda sola en cada request (credentials:'include' en api/client.ts).
//
//  Como el frontend no tiene el token ni los datos del usuario en storage, al
//  montar consulta GET /api/auth/me: si la cookie es valida, devuelve el perfil
//  y quedamos autenticados; si no, 401 y quedamos deslogueados.
//
//  Tema (claro/oscuro): es una preferencia POR USUARIO que vive en la BD. El
//  perfil la trae en `tema`; al cambiarla se persiste con PUT /api/auth/me/tema
//  y se aplica con el atributo data-theme en <html>. Cada usuario recupera su
//  preferencia al loguearse. Se cachea en localStorage solo para evitar el
//  parpadeo claro→oscuro al recargar (no es dato sensible).
// ============================================================================
import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useState,
  type ReactNode,
} from 'react';
import {
  login as apiLogin,
  logout as apiLogout,
  obtenerPerfil,
  guardarTema,
  type Rol,
  type Tema,
} from './authApi';

interface UsuarioSesion {
  id: number;
  nombre: string;
  email: string;
  rol: Rol;
  tema: Tema;
}

interface AuthContextValue {
  usuario: UsuarioSesion | null;
  estaAutenticado: boolean;
  cargando: boolean;          // login en progreso
  verificandoSesion: boolean; // chequeo inicial de la cookie al montar
  error: string;
  iniciarSesion: (email: string, password: string) => Promise<{ ok: boolean }>;
  cerrarSesion: () => void;
  cambiarTema: (tema: Tema) => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Aplica el tema al documento (data-theme en <html>) y lo cachea para el
// próximo arranque. CSS usa [data-theme="dark"] para el modo oscuro.
function aplicarTema(tema: Tema) {
  document.documentElement.setAttribute('data-theme', tema === 'oscuro' ? 'dark' : 'light');
  try { localStorage.setItem('tema', tema); } catch { /* storage no disponible */ }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioSesion | null>(null);
  const [cargando, setCargando] = useState(false);
  const [verificandoSesion, setVerificandoSesion] = useState(true);
  const [error, setError] = useState('');

  // Al montar: ¿hay sesion activa? Lo dice la cookie via GET /api/auth/me.
  useEffect(() => {
    let activo = true;
    obtenerPerfil()
      .then((p) => {
        if (activo) setUsuario({ id: p.id, nombre: p.nombre, email: p.email, rol: p.rol, tema: p.tema });
      })
      .catch(() => {
        if (activo) setUsuario(null);
      })
      .finally(() => {
        if (activo) setVerificandoSesion(false);
      });
    return () => { activo = false; };
  }, []);

  // Aplica el tema cuando cambia (login, recarga de perfil, toggle). Sin sesión
  // → claro (el login siempre se ve claro).
  useEffect(() => {
    aplicarTema(usuario?.tema ?? 'claro');
  }, [usuario?.tema]);

  const iniciarSesion = useCallback(
    async (email: string, password: string): Promise<{ ok: boolean }> => {
      setCargando(true);
      setError('');

      try {
        // 1. Login: el backend valida y setea la cookie HttpOnly con el JWT.
        await apiLogin({ email, password });

        // 2. Con la cookie ya puesta, traemos el perfil completo (incluye id y tema).
        const perfil = await obtenerPerfil();
        setUsuario({ id: perfil.id, nombre: perfil.nombre, email: perfil.email, rol: perfil.rol, tema: perfil.tema });
        return { ok: true };
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Credenciales incorrectas';
        // El backend devuelve "401 Unauthorized — Credenciales incorrectas":
        // limpiamos el prefijo del status code para una UX mas linda.
        setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
        setUsuario(null);
        return { ok: false };
      } finally {
        setCargando(false);
      }
    },
    [],
  );

  const cerrarSesion = useCallback(() => {
    // Borra la cookie en el server; no esperamos la respuesta para limpiar la UI.
    apiLogout().catch(() => {});
    setUsuario(null);
    setError('');
  }, []);

  // Cambia el tema de forma optimista (UI instantánea) y lo persiste en la BD.
  // Si la persistencia falla, revierte.
  const cambiarTema = useCallback((nuevo: Tema) => {
    setUsuario((prev) => (prev ? { ...prev, tema: nuevo } : prev));
    guardarTema(nuevo).catch(() => {
      const anterior: Tema = nuevo === 'oscuro' ? 'claro' : 'oscuro';
      setUsuario((prev) => (prev ? { ...prev, tema: anterior } : prev));
    });
  }, []);

  const value: AuthContextValue = {
    usuario,
    estaAutenticado: usuario !== null,
    cargando,
    verificandoSesion,
    error,
    iniciarSesion,
    cerrarSesion,
    cambiarTema,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Hook para consumir el contexto. Tira si se usa fuera del Provider —
 * eso indica un bug, no un fallo runtime que haya que tolerar.
 */
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth debe usarse dentro de un AuthProvider');
  }
  return ctx;
}
