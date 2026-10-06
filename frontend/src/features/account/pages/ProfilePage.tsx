import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { authApi, meQueryOptions } from '@/features/auth/api/auth.api'
import type { Me } from '@/features/auth/auth.types'
import { PasswordField } from '@/shared/components/PasswordField'
import { useAuth } from '@/features/auth/useAuth'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
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
      notify.success('Profile saved')
    } catch (error) {
      setFormError(
        applyServerErrors(form, error, { email_taken: 'email', current_password_wrong: 'currentPassword' }),
      )
    }
  })

  return (
    <div className="grid gap-6">
      <PageHeader title="My profile" description="Your name and email. Your mobile number is how you log in." />

      <form onSubmit={onSubmit} noValidate className="grid gap-6 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
        <header className="grid gap-0.5">
          <h2 className="text-base font-bold text-ink-900">Personal details</h2>
          <p className="text-xs text-ink-500">Changes show up across your account straight away.</p>
        </header>

        {formError && <FormAlert kind="error">{formError}</FormAlert>}

        {/* Two columns from tablet up; every field has its label on top, so the boxes in a row line up.
            The password box, when it appears, lands right under Email - the field it's for. */}
        <div className="grid gap-5 sm:grid-cols-2">
          <TextField label="Full name" autoComplete="name" error={errors.fullName?.message} {...form.register('fullName')} />
          <TextField
            label="Email"
            type="email"
            autoComplete="email"
            hint="Booking confirmations and password reset links go here."
            error={errors.email?.message}
            {...form.register('email')}
          />
          <TextField id="mobile" label="Mobile" value={user.phone} disabled readOnly hint="Your login number can't be changed online yet." />
          {emailChanged && (
            <div className="animate-fade-up">
              <PasswordField
                label="Current password"
                autoComplete="current-password"
                hint="Needed to change your email."
                error={errors.currentPassword?.message}
                {...form.register('currentPassword')}
              />
            </div>
          )}
        </div>

        {/* The save bar: a soft strip along the card's bottom edge, button on the right. */}
        <footer className="-mx-5 -mb-5 flex justify-end rounded-b-3xl border-t bg-ink-50/70 px-5 py-4 sm:-mx-6 sm:-mb-6 sm:px-6">
          <Button type="submit" disabled={isSubmitting}>
            {isSubmitting && <Spinner />}
            Save changes
          </Button>
        </footer>
      </form>
    </div>
  )
}
