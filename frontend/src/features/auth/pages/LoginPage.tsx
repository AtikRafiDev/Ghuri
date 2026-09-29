import { zodResolver } from '@hookform/resolvers/zod'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { homePathFor } from '../auth.types'
import { AuthCard } from '../components/AuthCard'
import { loginSchema, type LoginInput } from '../schemas/login.schema'
import { useAuth } from '../useAuth'

export function LoginPage() {
  const { status, user, login } = useAuth()
  const location = useLocation()
  // Set by RequireAuth: the page the user wanted before being sent here.
  const from = (location.state as { from?: string } | null)?.from
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { phoneOrEmail: '', password: '' },
  })
  const { errors, isSubmitting } = form.formState

  // Logged in - just now by this form, or already before: go on. No
  // navigate() call in onSubmit needed; this re-render does it.
  if (status === 'authenticated' && user) {
    return <Navigate to={from ?? homePathFor(user)} replace />
  }

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await login(values)
    } catch (error) {
      // "Wrong phone/email or password.", "Too many attempts..." etc.
      setFormError(applyServerErrors(form, error))
    }
  })

  return (
    <AuthCard
      title="Log in"
      description="With your phone number or email."
      footer={
        <span>
          New to Ghuri?{' '}
          <Link to="/register" className="font-medium text-foreground underline underline-offset-4">
            Create an account
          </Link>
        </span>
      }
    >
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField
          label="Phone or email"
          autoComplete="username"
          autoFocus
          error={errors.phoneOrEmail?.message}
          {...form.register('phoneOrEmail')}
        />
        <TextField
          label="Password"
          type="password"
          autoComplete="current-password"
          error={errors.password?.message}
          {...form.register('password')}
        />
        <Link to="/forgot-password" className="-mt-2 justify-self-end text-sm text-muted-foreground underline-offset-4 hover:underline">
          Forgot password?
        </Link>
        <Button type="submit" size="lg" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Log in
        </Button>
      </form>
    </AuthCard>
  )
}
