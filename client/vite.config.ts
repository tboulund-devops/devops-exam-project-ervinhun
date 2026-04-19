import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'

export default defineConfig({
  plugins: [react()],
  server: {
    host: '0.0.0.0',
    allowedHosts: [
      'todo.89.117.50.225.nip.io',
      '89.117.50.225',
      'localhost',
      '127.0.0.1',
      'todo.89.117.49.191.nip.io',
      '89.117.49.191'  
    ],
    port: 5173,
    proxy: {
      '/api': {
        target: 'http://api:5000',
        changeOrigin: true,
      }
    }
  }
})