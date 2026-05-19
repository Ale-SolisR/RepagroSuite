import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
  server: {
    port: 5173,
    proxy: {
      '/api': {
        target: 'https://localhost:7266',
        changeOrigin: true,
        secure: false,
      },
      // SignalR Hub para tiempo real. `ws: true` habilita el upgrade a WebSocket.
      '/hubs': {
        target: 'https://localhost:7266',
        changeOrigin: true,
        secure: false,
        ws: true,
      },
    },
  },
})
