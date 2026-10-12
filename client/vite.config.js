import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.js',
    // Tests never touch the network, whatever a developer's .env.local says.
    // The base URL is a placeholder that real-mode tests check is used.
    env: { VITE_USE_MOCK: 'true', VITE_API_BASE_URL: 'http://api.test' },
    // Each test starts with empty call history on every spy, and with any
    // variable a test changed through vi.stubEnv put back.
    clearMocks: true,
    unstubEnvs: true,
  },
})
