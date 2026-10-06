import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRightIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { SuccessBurst } from '@/shared/components/SuccessBurst'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { authApi } from '../api/auth.api'
import { AuthCard } from '../components/AuthCard'
import { resetPasswordSchema, type ResetPasswordInput } from '../schemas/password.schema'
import { PasswordField } from '@/shared/components/PasswordField'
import { authLinkClass } from '../components/authLink'

/** Opened from the email link: /reset-password?email=...&token=... */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const [done, setDone] = useState(false)
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<ResetPasswordInput>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })
  const { errors, isSubmitting } = form.formState

  const requestNewLink = (
    <Link to="/forgot-password" className={authLinkClass}>
      Request a new link
    </Link>
  )

  // Opened without the link's details (e.g. typed by hand, or the email
  // app cut the link in half).
  if (!email || !token) {
    return (
      <AuthCard title="Link incomplete" footer={requestNewLink}>
        <FormAlert kind="error">This reset link is missing some parts. Please request a new one.</FormAlert>
      </AuthCard>
    )
  }

  if (done) {
    return (
      <AuthCard title="Password changed">
        <div className="grid gap-5">
          <div role="status" className="grid justify-items-center gap-4 rounded-3xl bg-forest-50/60 px-6 py-8 text-center ring-1 ring-forest-100 ring-inset">
            <SuccessBurst size={72} />
            <p className="max-w-sm animate-[fade-up_0.6s_var(--ease-out-expo)_0.9s_backwards] text-sm text-ink-600">
              Your new password is set. For your safety, every device that was logged in has been logged out.
            </p>
          </div>
          <Button asChild size="lg">
            <Link to="/login">
              Log in
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </div>
      </AuthCard>
    )
  }

  const onSubmit = form.handleSubmit(async ({ newPassword }) => {
    setFormError(null)
    try {
      await authApi.resetPassword({ email, token, newPassword })
      setDone(true)
    } catch (error) {
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <AuthCard title="Set a new password" description={`For ${email}`} footer={requestNewLink}>
      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <PasswordField
          label="New password"
          autoComplete="new-password"
          autoFocus
          hint="At least 8 characters."
          error={errors.newPassword?.message}
          {...form.register('newPassword')}
        />
        <PasswordField
          label="Repeat new password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...form.register('confirmPassword')}
        />
        <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Save new password
        </Button>
      </form>
    </AuthCard>
  )
}
