import { CircleAlertIcon } from 'lucide-react'
import type { ComponentProps, ReactNode } from 'react'
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

  return (
    <div className="grid content-start gap-2">
      <Label htmlFor={inputId}>{label}</Label>
      <Input
        id={inputId}
        aria-invalid={error ? true : undefined}
        aria-describedby={error || hint ? messageId : undefined}
        {...inputProps}
      />
      <FieldMessage id={messageId} error={error} hint={hint} />
    </div>
  )
}

/** The line under a field: an error (terracotta, with an icon, sliding in) or a quiet hint. */
export function FieldMessage({ id, error, hint }: { id: string; error?: string; hint?: ReactNode }) {
  if (error) {
    return (
      <p id={id} className="flex animate-[fade-up_0.3s_var(--ease-out-expo)_backwards] items-start gap-1.5 text-[0.8125rem] font-medium text-destructive">
        <CircleAlertIcon className="mt-px size-3.5 shrink-0" />
        {error}
      </p>
    )
  }
  if (hint) {
    return (
      <p id={id} className="text-[0.8125rem] text-ink-500">
        {hint}
      </p>
    )
  }
  return null
}
