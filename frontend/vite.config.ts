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
    host: '0.0.0.0',
    strictPort: false,
    open: false,
    hmr: {
      overlay: true,
    },
    watch: {
      usePolling: true,
      interval: 1000,
      // Ignore directories that cause excessive file watching
      ignored: [
        '**/node_modules/**',
        '**/.git/**',
        '**/dist/**',
        '**/coverage/**',
        '**/.vite/**',
        '**/build/**',
        '**/.cache/**',
        '**/tmp/**',
        '**/temp/**',
      ],
    },
    proxy: {
      '/api': {
        target: 'http://localhost:5000',
        changeOrigin: true,
        secure: false,
        ws: false,
      },
    },
    fs: {
      // Restrict file system access to project directory only
      strict: true,
      allow: ['.'],
    },
  },
  build: {
    outDir: 'dist',
    // Reduce concurrent operations to prevent file handle exhaustion
    chunkSizeWarningLimit: 1000,
    rollupOptions: {
      output: {
        manualChunks: {
          'react-vendor': ['react', 'react-dom', 'react-router-dom'],
          'mui-vendor': [
            '@emotion/react',
            '@emotion/styled',
            '@mui/icons-material',
            '@mui/material',
            '@mui/x-date-pickers',
            'date-fns',
          ],
          'data-vendor': ['@tanstack/react-query', 'axios', 'recharts'],
        },
      },
    },
  },
  optimizeDeps: {
    // Re-enable dependency discovery but limit file watching
    noDiscovery: false,
    include: [
      'react',
      'react-dom',
      'react-router-dom',
      '@mui/material',
      '@mui/icons-material',
      'axios',
    ],
    // Exclude large dependencies from optimization
    exclude: ['@mui/x-data-grid'],
  },
})
