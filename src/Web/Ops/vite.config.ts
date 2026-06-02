import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

export default defineConfig({
  plugins: [react(), tailwindcss()],
  base: process.env.NODE_ENV === 'production' ? '/ops/' : '/',
  server: {
    port: 5174,
    proxy: {
      '/api': {
        target: 'http://localhost:5265',
        changeOrigin: true,
      },
      '/openapi': {
        target: 'http://localhost:5265',
        changeOrigin: true,
      },
    },
  },
  resolve: {
    alias: {
      '@': path.resolve(__dirname, './src'),
    },
  },
})
