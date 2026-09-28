import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { useState, type ReactNode } from 'react'

// Everything the whole app needs wrapped around it lives here (blueprint
// 13.1: app/providers.tsx). Later: the auth context, the router...
export function Providers({ children }: { children: ReactNode }) {
  // useState with a function = created ONCE for the app's lifetime. Creating
  // a QueryClient directly in the component body would make a new, empty
  // cache on every re-render and throw away all fetched data.
  const [queryClient] = useState(() => new QueryClient())

  return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>
}
