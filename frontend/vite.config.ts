import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import path from 'path'

const uiVendorPackages = [
  '@remix-run',
  '@emotion',
  '@floating-ui',
  '@mui',
  '@popperjs',
  'clsx',
  'date-fns',
  'dom-helpers',
  'react',
  'react-dom',
  'react-router',
  'react-router-dom',
  'react-transition-group',
  'scheduler',
  'stylis',
]
const dataVendorPackages = [
  '@apollo',
  '@tanstack',
  'axios',
  'graphql',
  'graphql-tag',
  'graphql-ws',
  'optimism',
  'subscriptions-transport-ws',
  'ts-invariant',
  'ws',
]
const chartVendorPackages = ['d3-', 'internmap', 'lodash', 'recharts']

function isNodeModulePackage(id: string, packages: string[]) {
  return packages.some(pkg => id.includes(`/node_modules/${pkg}`))
}

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
        manualChunks(id) {
          if (!id.includes('node_modules')) {
            return undefined
          }

          if (isNodeModulePackage(id, chartVendorPackages)) {
            return 'charts-vendor'
          }

          if (isNodeModulePackage(id, uiVendorPackages)) {
            return 'ui-vendor'
          }

          if (isNodeModulePackage(id, dataVendorPackages)) {
            return 'data-vendor'
          }

          return undefined
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
