import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Controller, useForm } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Spinner } from '@/components/ui/spinner'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { TextField } from '@/shared/components/TextField'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import { assignableStaffRoles, staffApi, staffKeys, staffRoleHints, staffRoleLabels, type StaffRole } from '../api/staff.api'
import { staffSchema, type StaffInput } from '../schemas/staff.schema'

type StaffFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  /** Called with the new person's email, to say where the welcome link went. */
  onCreated: (email: string) => void
}

/** "Add staff member": name, mobile, email and role. No password - they set their own from the emailed link. */
export function StaffFormDialog({ open, onOpenChange, onCreated }: StaffFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-lg">
        <DialogHeader>
          <DialogTitle>Add staff member</DialogTitle>
          <DialogDescription>
            They get an email with a link to set their own password, then log in with their email or mobile number.
          </DialogDescription>
        </DialogHeader>
        <StaffForm
          onDone={(email) => {
            onOpenChange(false)
            if (email) onCreated(email)
          }}
        />
      </DialogContent>
    </Dialog>
  )
}

function StaffForm({ onDone }: { onDone: (createdEmail?: string) => void }) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<StaffInput>({
    resolver: zodResolver(staffSchema),
    defaultValues: { fullName: '', phone: '', email: '', role: 3 },
  })
  const { errors, isSubmitting } = form.formState

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      await staffApi.create(values)
      await queryClient.invalidateQueries({ queryKey: staffKeys.all })
      onDone(values.email)
    } catch (error) {
      setFormError(applyServerErrors(form, error, { phone_taken: 'phone', email_taken: 'email' }))
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-4">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <TextField label="Full name" autoFocus autoComplete="off" error={errors.fullName?.message} {...form.register('fullName')} />
      <TextField
        label="Mobile number"
        type="tel"
        inputMode="tel"
        placeholder="01711000000"
        autoComplete="off"
        error={errors.phone?.message}
        hint="They can log in with it. A number that already has an account (e.g. their customer account) can't be used."
        {...form.register('phone')}
      />
      <TextField
        label="Email"
        type="email"
        autoComplete="off"
        error={errors.email?.message}
        hint="The welcome link and later password resets go here."
        {...form.register('email')}
      />

      <FormField label="Role" htmlFor="staff-role" error={errors.role?.message}>
        <Controller
          control={form.control}
          name="role"
          render={({ field }) => (
            <>
              <Select value={String(field.value)} onValueChange={(v) => field.onChange(Number(v) as StaffRole)}>
                <SelectTrigger id="staff-role" className="w-full">
                  <SelectValue />
                </SelectTrigger>
                <SelectContent>
                  {assignableStaffRoles.map((role) => (
                    <SelectItem key={role} value={String(role)}>
                      {staffRoleLabels[role]}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              <p className="text-sm text-muted-foreground">{staffRoleHints[field.value]}</p>
            </>
          )}
        />
      </FormField>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={() => onDone()} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          Add and send invite
        </Button>
      </DialogFooter>
    </form>
  )
}
