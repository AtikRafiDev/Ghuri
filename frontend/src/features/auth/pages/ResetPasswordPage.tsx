import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, useNavigate, useSearchParams } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { authApi } from '../api/auth.api'
import { AuthCard } from '../components/AuthCard'
import { resetPasswordSchema, type ResetPasswordInput } from '../schemas/password.schema'
import { PasswordField } from '@/shared/components/PasswordField'
import { authLinkClass } from '../components/authLink'
import { useAuth } from '../useAuth'

/** Opened from the email link: /reset-password?email=...&token=... (a forgotten password, or a new staff member's welcome link). */
export function ResetPasswordPage() {
  const [searchParams] = useSearchParams()
  const email = searchParams.get('email') ?? ''
  const token = searchParams.get('token') ?? ''
  const { logout } = useAuth()
  const navigate = useNavigate()
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

  const onSubmit = form.handleSubmit(async ({ newPassword }) => {
    setFormError(null)
    try {
      await authApi.resetPassword({ email, token, newPassword })
      // Whoever is logged in on THIS browser - e.g. the Super Admin who sent
      // the welcome link and opened it here - is not the person who just set
      // this password. End that session, or /login would see "already logged
      // in" and carry on as them. (The API ends it by its cookie alone; with
      // no one logged in this does nothing. If the API can't be reached, the
      // local logout still happens - and the password IS saved, so no error.)
      await logout().catch(() => undefined)
      // The toast outlives the redirect.
      notify.success('Password saved. Log in with your new password.')
      navigate('/login', { replace: true, state: { email } })
    } catch (error) {
      setFormError(applyServerErrors(form, error, { new_password_same_as_old: 'newPassword' }))
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
