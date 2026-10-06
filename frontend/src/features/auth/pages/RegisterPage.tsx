import { zodResolver } from '@hookform/resolvers/zod'
import { ArrowRightIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { homePathFor } from '../auth.types'
import { AuthCard } from '../components/AuthCard'
import { registerSchema, type RegisterInput } from '../schemas/register.schema'
import { useAuth } from '../useAuth'
import { PasswordField } from '@/shared/components/PasswordField'
import { authLinkClass } from '../components/authLink'

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
      notify.success('Welcome to Ghuri!', { description: 'Your account is ready - your trips will all live here.' })
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
          <Link to="/login" className={authLinkClass}>
            Log in
          </Link>
        </span>
      }
    >
      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField label="Full name" autoComplete="name" autoFocus error={errors.fullName?.message} {...form.register('fullName')} />
        {/* Pairs side by side once there's room; each field keeps its label on top, so the inputs line up. */}
        <div className="grid gap-5 sm:grid-cols-2">
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
        </div>
        <div className="grid gap-5 sm:grid-cols-2">
          <PasswordField
            label="Password"
            autoComplete="new-password"
            hint="At least 8 characters."
            error={errors.password?.message}
            {...form.register('password')}
          />
          <PasswordField
            label="Repeat password"
            autoComplete="new-password"
            error={errors.confirmPassword?.message}
            {...form.register('confirmPassword')}
          />
        </div>
        <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Create account
          {!isSubmitting && <ArrowRightIcon className="group-hover/button:translate-x-0.5" />}
        </Button>
      </form>
    </AuthCard>
  )
}
