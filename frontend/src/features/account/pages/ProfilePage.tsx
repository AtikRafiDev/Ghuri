import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { authApi, meQueryOptions } from '@/features/auth/api/auth.api'
import type { Me } from '@/features/auth/auth.types'
import { useAuth } from '@/features/auth/useAuth'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { profileSchema, type ProfileInput } from '../schemas/profile.schema'

/** /account/profile - change your name and email (17-day plan, Day 11: UpdateProfile). */
export function ProfilePage() {
  useDocumentMeta({ title: 'My profile' })
  const { user } = useAuth()
  if (!user) return null // RequireAuth guarantees a user; this only satisfies TypeScript

  return <ProfileForm user={user} />
}

function ProfileForm({ user }: { user: Me }) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)
  const [saved, setSaved] = useState(false)

  const form = useForm<ProfileInput>({
    resolver: zodResolver(profileSchema),
    defaultValues: { fullName: user.fullName, email: user.email ?? '', currentPassword: '' },
  })
  const { errors, isSubmitting } = form.formState

  // The password box appears only when the email changes - like the API,
  // which asks for it only then (reset links go to that email).
  const email = useWatch({ control: form.control, name: 'email' })
  const emailChanged = email.trim().toLowerCase() !== (user.email ?? '').toLowerCase()

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    setSaved(false)
    if (emailChanged && !values.currentPassword) {
      form.setError('currentPassword', { message: 'Enter your current password to change your email.' })
      return
    }
    try {
      await authApi.updateMe({
        fullName: values.fullName,
        email: values.email.trim() || null,
        currentPassword: emailChanged ? values.currentPassword : null,
      })
      await queryClient.invalidateQueries({ queryKey: meQueryOptions.queryKey }) // header and account show the new name
      form.reset({ ...values, currentPassword: '' })
      setSaved(true)
    } catch (error) {
      setFormError(
        applyServerErrors(form, error, { email_taken: 'email', current_password_wrong: 'currentPassword' }),
      )
    }
  })

  return (
    <div className="grid max-w-md gap-6">
      <h1 className="text-2xl font-semibold">My profile</h1>
      <form onSubmit={onSubmit} noValidate className="grid gap-4">
        {saved && <FormAlert kind="success">Your profile is saved.</FormAlert>}
        {formError && <FormAlert kind="error">{formError}</FormAlert>}
        <TextField label="Full name" autoComplete="name" error={errors.fullName?.message} {...form.register('fullName')} />
        <TextField
          label="Email"
          type="email"
          autoComplete="email"
          hint="Booking confirmations and password reset links go here."
          error={errors.email?.message}
          {...form.register('email')}
        />
        {emailChanged && (
          <TextField
            label="Current password"
            type="password"
            autoComplete="current-password"
            hint="Needed to change your email."
            error={errors.currentPassword?.message}
            {...form.register('currentPassword')}
          />
        )}
        <TextField id="mobile" label="Mobile" value={user.phone} disabled readOnly hint="Your login number can't be changed online yet." />
        <div>
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Spinner />}
            Save
          </Button>
        </div>
      </form>
    </div>
  )
}
