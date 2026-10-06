import type { ReactNode } from 'react'
import { Label } from '@/components/ui/label'
import { FieldMessage } from './TextField'

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
  return (
    <div className="grid content-start gap-2">
      <Label htmlFor={htmlFor}>{label}</Label>
      {children}
      <FieldMessage id={`${htmlFor}-message`} error={error} hint={hint} />
    </div>
  )
}
