import { defineConfig, loadEnv } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

// El backend al que apunta el proxy de dev. Por defecto el local (https://localhost:7266);
// define VITE_API_TARGET en .env.local para apuntar al backend publicado (anytempurl).
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')
  const apiTarget = env.VITE_API_TARGET || 'https://localhost:7266'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      port: 5173,
      host: true,          // escucha en 0.0.0.0 para poder probar desde el teléfono (LAN)
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
        },
        // SignalR Hub para tiempo real. `ws: true` habilita el upgrade a WebSocket.
        '/hubs': {
          target: apiTarget,
          changeOrigin: true,
          secure: false,
          ws: true,
        },
      },
    },
  }
})
