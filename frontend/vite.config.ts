import path from 'node:path'
import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// The .NET API's address when started with plain `dotnet run` (the "http"
// profile in backend/src/Ghuri.Api/Properties/launchSettings.json).
const apiTarget = 'http://localhost:5176'

// https://vite.dev/config/
export default defineConfig({
  // tailwindcss(): turns the utility classes used in components
  // (e.g. "p-4 text-sm") into real CSS - only the ones actually used.
  plugins: [react(), tailwindcss()],
  resolve: {
    // "@/features/auth/..." instead of "../../../features/auth/..."
    // (coding standards: imports through the @/ alias). tsconfig.app.json
    // declares the same alias so TypeScript understands it too.
    alias: { '@': path.resolve(import.meta.dirname, './src') },
  },
  server: {
    // Vite answers only requests addressed to localhost unless told
    // otherwise. ".trycloudflare.com" = any Cloudflare quick-tunnel address
    // (README "Public address"): the tunnel lets SSLCommerz reach this PC.
    allowedHosts: ['.trycloudflare.com'],
    // Dev proxy: the browser only ever talks to Vite (localhost:5173), and
    // Vite forwards these paths to the API server-to-server. To the browser
    // everything is ONE origin, so no CORS setup is needed - and the
    // refresh cookie is simply a same-site cookie. Nginx does the same job
    // in production.
    proxy: {
      '/api': { target: apiTarget, changeOrigin: true },
      '/health': { target: apiTarget, changeOrigin: true },
      // Uploaded images (backend Storage:PublicBaseUrl).
      '/files': { target: apiTarget, changeOrigin: true },
    },
  },
})
