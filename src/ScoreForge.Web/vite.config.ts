/// <reference types="vitest/config" />
import fs from 'node:fs'
import path from 'node:path'
import { fileURLToPath } from 'node:url'
import { defineConfig, type PluginOption } from 'vite'
import react from '@vitejs/plugin-react'
import tailwindcss from '@tailwindcss/vite'

const repoRoot = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '../..')
const certDir = path.join(repoRoot, '.dev-certs')
const viteCert = path.join(certDir, 'vite.pem')
const viteKey = path.join(certDir, 'vite-key.pem')
const hasMkcertCerts = fs.existsSync(viteCert) && fs.existsSync(viteKey)
const useHttps = process.env.SCOREFORGE_HTTPS === '1'

const apiTarget = 'https://127.0.0.1:7016'

const apiProxy = {
  target: apiTarget,
  changeOrigin: true,
  secure: false,
  // So the API builds OAuth redirect_uri against the Vite origin (not :7016).
  xfwd: true,
}

async function loadPlugins(): Promise<PluginOption[]> {
  const plugins: PluginOption[] = [react(), tailwindcss()]

  // Self-signed fallback when mkcert certs are not present.
  if (useHttps && !hasMkcertCerts)
  {
    const { default: basicSsl } = await import('@vitejs/plugin-basic-ssl')
    plugins.push(basicSsl())
  }

  return plugins
}

export default defineConfig(async () => ({
  plugins: await loadPlugins(),
  server: {
    port: 5173,
    https: hasMkcertCerts
      ? {
          cert: fs.readFileSync(viteCert),
          key: fs.readFileSync(viteKey),
        }
      : useHttps,
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
}))
