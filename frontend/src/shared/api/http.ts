import axios from 'axios'

// The ONE axios instance every API call goes through (blueprint 13.1:
// shared/api/http.ts). Having a single instance means cross-cutting
// behaviour gets added in exactly one place later - attaching the login
// token, silently refreshing it on 401, turning ProblemDetails errors into
// typed errors (blueprint 13.2).
export const http = axios.create({
  // Empty base URL = "same address the page came from". In development
  // that's Vite, whose proxy forwards to the API; in production it'll be
  // Nginx doing the same. No hard-coded server address in app code.
  baseURL: '',
  timeout: 15_000,
})
