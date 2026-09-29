import { CircleAlertIcon, CircleCheckIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'

/** One message above a form: what went wrong (error) or what happened (success). */
export function FormAlert({ kind, children }: { kind: 'error' | 'success'; children: ReactNode }) {
  return (
    <Alert variant={kind === 'error' ? 'destructive' : 'default'} role={kind === 'error' ? 'alert' : 'status'}>
      {kind === 'error' ? <CircleAlertIcon /> : <CircleCheckIcon />}
      <AlertDescription>{children}</AlertDescription>
    </Alert>
  )
}
