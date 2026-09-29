import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { homePathFor } from '../auth.types'
import { AuthCard } from '../components/AuthCard'
import { registerSchema, type RegisterInput } from '../schemas/register.schema'
import { useAuth } from '../useAuth'

export function RegisterPage() {
  const { status, user, register } = useAuth()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: { fullName: '', phone: '', email: '', password: '', confirmPassword: '' },
  })
  const { errors, isSubmitting } = form.formState

  // The API logs a new customer straight in - so after register() this
  // re-render finds them authenticated and moves on.
  if (status === 'authenticated' && user) {
    return <Navigate to={homePathFor(user)} replace />
  }

  const onSubmit = form.handleSubmit(async ({ fullName, phone, email, password }) => {
    setFormError(null)
    try {
      await register({ fullName, phone, email: email || null, password })
    } catch (error) {
      // "Already registered" answers belong under their own field.
      setFormError(applyServerErrors(form, error, { phone_taken: 'phone', email_taken: 'email' }))
    }
  })

  return (
    <AuthCard
      title="Create your account"
      description="Book tours and keep all your trips in one place."
      footer={
        <span>
          Already have an account?{' '}
          <Link to="/login" className="font-medium text-foreground underline underline-offset-4">
            Log in
          </Link>
        </span>
      }
    >
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField label="Full name" autoComplete="name" autoFocus error={errors.fullName?.message} {...form.register('fullName')} />
        <TextField
          label="Mobile number"
          type="tel"
          autoComplete="tel"
          placeholder="01711000000"
          error={errors.phone?.message}
          {...form.register('phone')}
        />
        <TextField
          label="Email (optional)"
          type="email"
          autoComplete="email"
          hint="For your booking vouchers and password resets."
          error={errors.email?.message}
          {...form.register('email')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="new-password"
          hint="At least 8 characters."
          error={errors.password?.message}
          {...form.register('password')}
        />
        <TextField
          label="Repeat password"
          type="password"
          autoComplete="new-password"
          error={errors.confirmPassword?.message}
          {...form.register('confirmPassword')}
        />
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Create account
        </Button>
      </form>
    </AuthCard>
  )
}
