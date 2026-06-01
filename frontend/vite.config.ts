import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// El backend corre por default en https://localhost:7xxx o http://localhost:5180.
// Para evitar problemas de CORS y de cert HTTPS en dev, proxeamos /api al
// backend HTTP (puerto 5180). El backend ya tiene CORS habilitado para
// http://localhost:5173 igualmente, así que cualquiera de las dos vías
// (proxy o directa) funciona.
export default defineConfig({
  plugins: [react()],
  // Sirve los archivos de `assets/` desde la raíz tanto en dev como en build.
  // Reemplaza el default `public/` para mantener los estáticos en la carpeta
  // que el equipo ya está usando.
  publicDir: 'assets',
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://localhost:5180',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});
