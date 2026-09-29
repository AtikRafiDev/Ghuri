import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { authApi } from '../api/auth.api'
import { AuthCard } from '../components/AuthCard'
import { forgotPasswordSchema, type ForgotPasswordInput } from '../schemas/password.schema'

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
    <Link to="/login" className="font-medium text-foreground underline underline-offset-4">
      Back to log in
    </Link>
  )

  // The API answers the same for every email, registered or not - so the
  // page can't honestly say more than "IF an account exists".
  if (sentTo) {
    return (
      <AuthCard title="Check your email" footer={backToLogin}>
        <FormAlert kind="success">
          If an account exists for <strong>{sentTo}</strong>, we've sent a link to set a new password. It works once
          and expires soon.
        </FormAlert>
      </AuthCard>
    )
  }

  return (
    <AuthCard title="Forgot your password?" description="We'll email you a link to set a new one." footer={backToLogin}>
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField label="Email" type="email" autoComplete="email" autoFocus error={errors.email?.message} {...form.register('email')} />
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Send reset link
        </Button>
      </form>
    </AuthCard>
  )
}
