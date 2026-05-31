// ============================================================================
//  Contexto global de autenticacion.
//
//  Persistencia: sessionStorage. Se borra al cerrar el browser (mas seguro
//  que localStorage para credenciales).
//
//  ⚠️ Nota: el id NO viene en LoginResponse — se obtiene haciendo GET /api/auth/me
//  inmediatamente despues del login. Necesitamos el id para el ABM
//  (saber que NO podes desactivarte a vos mismo, por ejemplo).
// ============================================================================
import {
  createContext,
  useCallback,
  useContext,
  useState,
  type ReactNode,
} from 'react';
import {
  login as apiLogin,
  obtenerPerfil,
  type LoginResponse,
  type Rol,
} from './authApi';

interface UsuarioSesion {
  id: number;
  nombre: string;
  email: string;
  rol: Rol;
  expiracion: string;
}

interface AuthContextValue {
  usuario: UsuarioSesion | null;
  estaAutenticado: boolean;
  cargando: boolean;
  error: string;
  iniciarSesion: (email: string, password: string) => Promise<{ ok: boolean }>;
  cerrarSesion: () => void;
}

const AuthContext = createContext<AuthContextValue | null>(null);

// Helper para leer la sesion inicial sin romper si JSON.parse falla
function leerSesionInicial(): UsuarioSesion | null {
  try {
    const guardado = sessionStorage.getItem('usuario');
    return guardado ? (JSON.parse(guardado) as UsuarioSesion) : null;
  } catch {
    return null;
  }
}

export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioSesion | null>(leerSesionInicial);
  const [cargando, setCargando] = useState(false);
  const [error, setError] = useState('');

  const iniciarSesion = useCallback(
    async (email: string, password: string): Promise<{ ok: boolean }> => {
      setCargando(true);
      setError('');

      try {
        // 1. Login — guardamos el token primero
        const resp: LoginResponse = await apiLogin({ email, password });
        sessionStorage.setItem('token', resp.token);

        // 2. Inmediatamente despues, GET /api/auth/me para obtener el id
        //    (el LoginResponse no lo trae)
        const perfil = await obtenerPerfil();

        const datos: UsuarioSesion = {
          id: perfil.id,
          nombre: resp.nombre,
          email: resp.email,
          rol: resp.rol,
          expiracion: resp.expiracion,
        };
        sessionStorage.setItem('usuario', JSON.stringify(datos));

        setUsuario(datos);
        return { ok: true };
      } catch (err) {
        const msg = err instanceof Error ? err.message : 'Credenciales incorrectas';
        // El backend devuelve "401 Unauthorized — Credenciales incorrectas"
        // Limpiamos el prefijo del status code para una UX mas linda.
        setError(msg.replace(/^\d+\s+\w+\s+—\s+/, ''));
        // Si fallo el /me despues del login, limpiar el token
        sessionStorage.removeItem('token');
        return { ok: false };
      } finally {
        setCargando(false);
      }
    },
    [],
  );

  const cerrarSesion = useCallback(() => {
    sessionStorage.removeItem('token');
    sessionStorage.removeItem('usuario');
    setUsuario(null);
    setError('');
  }, []);

  const value: AuthContextValue = {
    usuario,
    estaAutenticado: usuario !== null,
    cargando,
    error,
    iniciarSesion,
    cerrarSesion,
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