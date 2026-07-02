import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';

// Anti-flash: aplica el tema cacheado del último usuario ANTES de renderizar,
// para no mostrar claro y saltar a oscuro tras resolver /api/auth/me.
// El AuthContext reconcilia con la preferencia real del usuario al cargar.
const temaCacheado = localStorage.getItem('tema');
document.documentElement.setAttribute(
  'data-theme',
  temaCacheado === 'oscuro' ? 'dark' : 'light',
);

const root = document.getElementById('root');
if (!root) throw new Error('No se encontró el contenedor #root');

createRoot(root).render(
  <StrictMode>
    <App />
  </StrictMode>
);
