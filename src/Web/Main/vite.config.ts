import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'
import path from 'path'

function preloadStylesheetPlugin() {
  return {
    name: 'preload-stylesheet',
    apply: 'build' as const,
    transformIndexHtml: {
      order: 'post' as const,
      handler(html: string) {
        return html.replace(
          /<link\b[^>]*\brel="stylesheet"[^>]*>/g,
          (match) => {
            const hrefMatch = /\bhref="([^"]+\.css)"/.exec(match)
            if (!hrefMatch) return match
            const href = hrefMatch[1]
            return `<link rel="preload" as="style" crossorigin href="${href}" onload="this.onload=null;this.rel='stylesheet'"><noscript><link rel="stylesheet" crossorigin href="${href}"></noscript>`
          },
        )
      },
    },
  }
}

export default defineConfig({
  plugins: [react(), tailwindcss(), preloadStylesheetPlugin()],
  build: {
    rollupOptions: {
      output: {
        manualChunks: {
          'vendor-react': ['react', 'react-dom', 'react-router'],
          'vendor-query': ['@tanstack/react-query'],
          'vendor-markdown': ['react-markdown', 'remark-gfm'],
        },
      },
    },
  },
  server: {
    port: 5173,
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
  ssr: {
    noExternal: ['react-router', '@tanstack/react-query'],
  },
})
