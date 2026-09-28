import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The .NET API's address when started with plain `dotnet run` (the "http"
// profile in backend/src/Ghuri.Api/Properties/launchSettings.json).
const apiTarget = 'http://localhost:5176'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: {
    // Dev proxy: the browser only ever talks to Vite (localhost:5173), and
    // Vite forwards these paths to the API server-to-server. To the browser
    // everything is ONE origin, so no CORS setup is needed - the same job
    // Nginx will do in production.
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
    },
  },
})
