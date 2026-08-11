import { fileURLToPath, URL } from 'node:url'

import react from '@vitejs/plugin-react'
import { defineConfig } from 'vitest/config'

// Kept separate from vite.config.ts: that config is in function form (it reads env to build the
// dev proxy), and merging into it is more fragile than repeating the alias here.
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: { '@': fileURLToPath(new URL('./src', import.meta.url)) },
  },
  test: {
    environment: 'jsdom',
    // Pinned so the origin that relative fetches resolve against is stable and matches the
    // URLs the msw handlers register. Vitest's default (localhost:3000) is not guaranteed.
    environmentOptions: { jsdom: { url: 'http://localhost' } },
    globals: true,
    setupFiles: ['./src/test/setup.ts'],
    css: false,
  },
})
