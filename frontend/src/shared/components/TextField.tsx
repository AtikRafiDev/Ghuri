import type { ComponentProps } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'

type TextFieldProps = ComponentProps<typeof Input> & {
  label: string
  /** Shown under the input in red; also marks the input invalid for screen readers. */
  error?: string
  hint?: string
}

/**
 * Label + input + message, the same way on every form. Spread React Hook
 * Form's register() into it: <TextField label="Email" {...form.register('email')} />
 */
export function TextField({ label, error, hint, id, ...inputProps }: TextFieldProps) {
  const inputId = id ?? inputProps.name
  const messageId = `${inputId}-message`
  const message = error ?? hint

  return (
    <div className="grid gap-1.5">
      <Label htmlFor={inputId}>{label}</Label>
      <Input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={message ? messageId : undefined}
        {...inputProps}
      />
      {message && (
        <p id={messageId} className={error ? 'text-sm text-destructive' : 'text-sm text-muted-foreground'}>
          {message}
        </p>
      )}
    </div>
  )
}
