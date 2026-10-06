import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { ArrowRightIcon } from 'lucide-react'
import { useState } from 'react'
import { useForm } from 'react-hook-form'
import { Link, Navigate, useLocation } from 'react-router'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { PasswordField } from '@/shared/components/PasswordField'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { meQueryOptions } from '../api/auth.api'
import { homePathFor } from '../auth.types'
import { AuthCard } from '../components/AuthCard'
import { authLinkClass } from '../components/authLink'
import { loginSchema, type LoginInput } from '../schemas/login.schema'
import { useAuth } from '../useAuth'

export function LoginPage() {
  const { status, user, login } = useAuth()
  const queryClient = useQueryClient()
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
      // login() has already loaded the profile - greet by first name. The toast outlives the redirect.
      const firstName = queryClient.getQueryData(meQueryOptions.queryKey)?.fullName.split(' ')[0]
      notify.success(firstName ? `Welcome back, ${firstName}!` : 'Welcome back!')
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
          <Link to="/register" className={authLinkClass}>
            Create an account
          </Link>
        </span>
      }
    >
      <form onSubmit={onSubmit} noValidate className="grid gap-5">
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField
          label="Phone or email"
          autoComplete="username"
          autoFocus
          error={errors.phoneOrEmail?.message}
          {...form.register('phoneOrEmail')}
        />
        <PasswordField
          label="Password"
          autoComplete="current-password"
          error={errors.password?.message}
          labelAside={
            <Link to="/forgot-password" className="text-[0.8125rem] leading-none font-semibold text-forest-700 underline-offset-4 transition-colors hover:text-forest-800 hover:underline">
              Forgot password?
            </Link>
          }
          {...form.register('password')}
        />
        <Button type="submit" size="lg" className="mt-1" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Log in
          {!isSubmitting && <ArrowRightIcon className="group-hover/button:translate-x-0.5" />}
        </Button>
      </form>
    </AuthCard>
  )
}
