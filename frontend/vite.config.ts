import { fileURLToPath, URL } from 'node:url'

import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig, loadEnv } from 'vite'

// https://vite.dev/config/
export default defineConfig(({ mode }) => {
  const env = loadEnv(mode, process.cwd(), '')

  // The .NET API does not enable CORS (see backend Program.cs), so in development every
  // /api call is proxied through the Vite dev server and reaches the backend same-origin.
  const apiTarget = env.VITE_API_PROXY_TARGET ?? 'http://localhost:5160'

  return {
    plugins: [react(), tailwindcss()],
    resolve: {
      alias: {
        '@': fileURLToPath(new URL('./src', import.meta.url)),
      },
    },
    server: {
      port: 5173,
      proxy: {
        '/api': {
          target: apiTarget,
          changeOrigin: true,
          // The https launch profile uses a self-signed development certificate.
          secure: false,
        },
      },
    },
  }
})
