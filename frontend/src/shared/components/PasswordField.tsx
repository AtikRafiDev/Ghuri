import { EyeIcon, EyeOffIcon } from 'lucide-react'
import { useState, type ComponentProps, type ReactNode } from 'react'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { cn } from '@/lib/utils'
import { FieldMessage } from './TextField'

type PasswordFieldProps = Omit<ComponentProps<typeof Input>, 'type'> & {
  label: string
  /** Shown under the input in red; also marks the input invalid for screen readers. */
  error?: string
  hint?: string
  /** A small link on the label's line, at the right - e.g. "Forgot password?". */
  labelAside?: ReactNode
}

/**
 * TextField for passwords: the same label + input + message, plus an eye
 * button inside the box that shows what's typed (handy on a phone keyboard).
 * Login, register, reset-password and profile use it. The label's
 * side link comes AFTER the input in the page, so Tab goes password →
 * "Forgot password?" → Log in, while the grid draws the link on the label's line.
 */
export function PasswordField({ label, error, hint, labelAside, id, className, ...inputProps }: PasswordFieldProps) {
  const [visible, setVisible] = useState(false)
  const inputId = id ?? inputProps.name
  const messageId = `${inputId}-message`

  return (
    <div className="grid grid-cols-[minmax(0,1fr)_auto] content-start items-center gap-x-3 gap-y-2 [&>p]:col-span-2">
      <Label htmlFor={inputId} className="col-start-1 row-start-1">
        {label}
      </Label>
      <div className="relative col-span-2 row-start-2">
        <Input
          id={inputId}
          type={visible ? 'text' : 'password'}
          aria-invalid={error ? true : undefined}
          aria-describedby={error || hint ? messageId : undefined}
          // pr-11 keeps typing clear of the eye button; Edge's own reveal button is hidden so there's only one.
          className={cn('pr-11 [&::-ms-clear]:hidden [&::-ms-reveal]:hidden', className)}
          {...inputProps}
        />
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label="Show password"
          aria-pressed={visible}
          aria-controls={inputId}
          className="absolute top-1/2 right-1.5 flex size-8 -translate-y-1/2 items-center justify-center rounded-lg text-ink-400 transition-colors duration-200 hover:bg-forest-50 hover:text-forest-700 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none"
        >
          {visible ? <EyeOffIcon className="size-4" /> : <EyeIcon className="size-4" />}
        </button>
      </div>
      {labelAside && <div className="col-start-2 row-start-1 flex justify-end">{labelAside}</div>}
      <FieldMessage id={messageId} error={error} hint={hint} />
    </div>
  )
}
