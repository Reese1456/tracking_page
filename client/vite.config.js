import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    setupFiles: './src/test/setup.js',
    // Tests never touch the network, whatever a developer's .env.local says.
    env: { VITE_USE_MOCK: 'true' },
    // Each test starts with empty call history on every spy.
    clearMocks: true,
  },
})
