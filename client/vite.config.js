import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  // The API's CORS policy allows exactly http://localhost:5173. If the port
  // is busy, failing to start is clearer than moving to 5174 and having every
  // request blocked by CORS.
  server: { port: 5173, strictPort: true },
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.js',
    // Tests never touch the network, whatever a developer's .env.local says.
    // The base URL is a placeholder that real-mode tests check is used.
    //
    // TZ is a zone behind UTC and unlike South Africa's, so a date formatter
    // that forgets its timeZone option and follows the computer's clock shows
    // the wrong time or day and fails. Left to the machine, CI runs in UTC and
    // central Europe in summer is UTC+2, the same as Johannesburg, which
    // would hide that mistake.
    env: { VITE_USE_MOCK: 'true', VITE_API_BASE_URL: 'http://api.test', TZ: 'America/Denver' },
    // Each test file runs in its own child process, which picks up TZ when it
    // starts. Worker threads share the parent's time zone and would ignore it.
    // This is Vitest's default; it's stated because the TZ setting needs it.
    pool: 'forks',
    // Each test starts with empty call history on every spy, and with any
    // variable or global a test changed through vi.stubEnv or vi.stubGlobal
    // put back.
    clearMocks: true,
    unstubEnvs: true,
    unstubGlobals: true,
  },
})
