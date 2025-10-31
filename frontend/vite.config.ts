import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      '@': path.resolve(__dirname, 'src'),
    },
  },
  server: {
    port: 3002,
    host: 'localhost',
    strictPort: false,
    open: false,
    hmr: {
      overlay: false,
      port: 3001,
      protocol: 'ws',
    },
    watch: {
      usePolling: true,
      interval: 300,
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        ws: false,
      },
    },
  },
  build: {
    outDir: 'dist',
  },
  optimizeDeps: {
    include: [
      'react', 
      'react-dom',
      '@mui/material',
      '@mui/icons-material',
      'axios',
      'date-fns',
      '@tanstack/react-query',
      'react-router-dom',
      'recharts',
    ],
  },
})