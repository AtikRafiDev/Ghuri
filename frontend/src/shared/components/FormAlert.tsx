import { CircleAlertIcon, CircleCheckIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'

/**
 * A message that belongs IN the page, next to what it's about: a form's
 * error (it shakes in so it's noticed), or a state that must stay visible
 * ("This booking was cancelled"). For "it worked!" after an action, use a
 * toast instead (shared/lib/notify.tsx).
 */
export function FormAlert({ kind, children }: { kind: 'error' | 'success'; children: ReactNode }) {
  return (
    <Alert variant={kind === 'error' ? 'destructive' : 'default'} role={kind === 'error' ? 'alert' : 'status'}>
      {kind === 'error' ? <CircleAlertIcon /> : <CircleCheckIcon />}
      <AlertDescription>{children}</AlertDescription>
    </Alert>
  )
}
