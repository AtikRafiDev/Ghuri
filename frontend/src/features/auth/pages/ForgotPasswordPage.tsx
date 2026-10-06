import { zodResolver } from '@hookform/resolvers/zod'
import { MailSearchIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { SuccessBurst } from '@/shared/components/SuccessBurst'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { authApi } from '../api/auth.api'
import { AuthCard } from '../components/AuthCard'
import { forgotPasswordSchema, type ForgotPasswordInput } from '../schemas/password.schema'
import { authLinkClass } from '../components/authLink'

export function ForgotPasswordPage() {
  const [sentTo, setSentTo] = useState<string | null>(null)
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<ForgotPasswordInput>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  })
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async ({ email }) => {
    setFormError(null)
    try {
      await authApi.forgotPassword(email)
      setSentTo(email)
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  const backToLogin = (
    <Link to="/login" className={authLinkClass}>
      Back to log in
    </Link>
  )

  // The API answers the same for every email, registered or not - so the
  // page can't honestly say more than "IF an account exists".
  if (sentTo) {
    return (
      <AuthCard title="Check your email" footer={backToLogin}>
        <div className="grid gap-4">
          <div role="status" className="grid justify-items-center gap-4 rounded-3xl bg-forest-50/60 px-6 py-8 text-center ring-1 ring-forest-100 ring-inset">
            <SuccessBurst size={72} />
            <p className="max-w-sm animate-[fade-up_0.6s_var(--ease-out-expo)_0.9s_backwards] text-sm text-ink-600">
              If an account exists for <strong className="font-semibold break-all text-ink-900">{sentTo}</strong>, we've sent a link to set
              a new password. It works once and expires soon.
            </p>
          </div>
          <p className="flex items-center justify-center gap-2 text-sm text-ink-500">
            <MailSearchIcon className="size-4 shrink-0 text-ink-400" />
            Not in your inbox? Check the spam folder.
          </p>
        </div>
      </AuthCard>
    )
  }

  return (
    <AuthCard title="Forgot your password?" description="We'll email you a link to set a new one." footer={backToLogin}>
      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField label="Email" type="email" autoComplete="email" autoFocus error={errors.email?.message} {...form.register('email')} />
        <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Send reset link
        </Button>
      </form>
    </AuthCard>
  )
}
