import type { ReactNode } from 'react'
import { Label } from '@/components/ui/label'

type FormFieldProps = {
  label: string
  /** The id of the control inside, so clicking the label focuses it. */
  htmlFor: string
  error?: string
  hint?: ReactNode
  children: ReactNode
}

/**
 * Label + any control + message - the same look as TextField, for controls
 * TextField can't wrap (select, textarea, checkbox, image upload).
 */
export function FormField({ label, htmlFor, error, hint, children }: FormFieldProps) {
  const message = error ?? hint
  return (
    <div className="grid gap-1.5">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      {message && (
        <p id={`${htmlFor}-message`} className={error ? 'text-sm text-destructive' : 'text-sm text-muted-foreground'}>
          {message}
        </p>
      )}
    </div>
  )
}
