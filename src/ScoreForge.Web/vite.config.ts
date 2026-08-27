/// <reference types="vitest/config" />
import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const apiTarget = 'https://127.0.0.1:7016'

const apiProxy = {
  target: apiTarget,
  changeOrigin: true,
  secure: false,
  // So the API builds OAuth redirect_uri against the Vite origin (not :7016).
  xfwd: true,
}

export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    port: 5173,
    proxy: {
      '/api': apiProxy,
      '/hubs': {
        ...apiProxy,
        ws: true,
      },
      // OAuth callbacks must share the SPA origin with the correlation cookie.
      '/signin-microsoft': apiProxy,
      '/signin-google': apiProxy,
    },
  },
  test: {
    environment: 'jsdom',
    globals: true,
    setupFiles: './src/test/setup.ts',
  },
})
