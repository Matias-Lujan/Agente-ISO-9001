import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    port: 5173,
    proxy: {
      // Cualquier llamada a /api/* desde el frontend
      // se redirige al backend en localhost:5180.
      // Esto evita problemas de CORS en desarrollo.
      '/api': {
        target: 'http://localhost:5180',
        changeOrigin: true,
        secure: false,
      },
    },
  },
});