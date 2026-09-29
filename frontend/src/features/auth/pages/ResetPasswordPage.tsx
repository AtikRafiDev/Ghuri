import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { authApi } from '../api/auth.api'
import { AuthCard } from '../components/AuthCard'
import { resetPasswordSchema, type ResetPasswordInput } from '../schemas/password.schema'

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
    <Link to="/forgot-password" className="font-medium text-foreground underline underline-offset-4">
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
        <div className="grid gap-4">
          <FormAlert kind="success">
            Your new password is set. For your safety, every device that was logged in has been logged out.
          </FormAlert>
          <Button asChild size="lg">
            <Link to="/login">Log in</Link>
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
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField
          label="New password"
          type="password"
          autoComplete="new-password"
          autoFocus
          hint="At least 8 characters."
          error={errors.newPassword?.message}
          {...form.register('newPassword')}
        />
        <TextField
          label="Repeat new password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...form.register('confirmPassword')}
        />
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Save new password
        </Button>
      </form>
    </AuthCard>
  )
}
