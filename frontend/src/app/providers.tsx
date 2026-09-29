import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'
import { AuthProvider } from '@/features/auth/AuthProvider'
import { AppError } from '@/shared/api/problem'

// Retrying helps with a flaky connection (network error, 5xx), but a 4xx
// ("not found", "not allowed", "invalid input") will give the same answer
// every time - retrying only makes the user wait longer.
function shouldRetry(failureCount: number, error: unknown): boolean {
  const isClientError = error instanceof AppError && error.status >= 400 && error.status < 500
  return !isClientError && failureCount < 2
}

// Everything the whole app needs wrapped around it (blueprint 13.1:
// app/providers.tsx). Order matters: AuthProvider uses the QueryClient,
// so it must sit inside QueryClientProvider.
export function Providers({ children }: { children: ReactNode }) {
  // useState with a function = created ONCE for the app's lifetime. Creating
  // a QueryClient directly in the component body would make a new, empty
  // cache on every re-render and throw away all fetched data.
  const [queryClient] = useState(() => new QueryClient({ defaultOptions: { queries: { retry: shouldRetry } } }))

  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>{children}</AuthProvider>
    </QueryClientProvider>
  )
}
