import { zodResolver } from '@hookform/resolvers/zod'
import { useQueryClient } from '@tanstack/react-query'
import { useId, useState, type ReactNode } from 'react'
import { useForm, useWatch } from 'react-hook-form'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { FormAlert } from '@/shared/components/FormAlert'
import { TextField } from '@/shared/components/TextField'
import { notify } from '@/shared/lib/notify'
import { applyServerErrors } from '@/shared/lib/serverErrors'
import {
  addDays,
  formatDate,
  packageKeys,
  packagesApi,
  todayInBangladesh,
  type AdminDeparture,
  type AdminPackage,
} from '../api/packages.api'
import { departureSchema, toDepartureForm, toDepartureRequest, type DepartureInput } from '../schemas/departure.schema'

type DepartureFormDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  pkg: AdminPackage
  /** Present = edit this one; absent = add a new one. */
  departure?: AdminDeparture
}

/** Add / edit one departure date in a pop-up, without leaving the Departures tab. */
export function DepartureFormDialog({ open, onOpenChange, pkg, departure }: DepartureFormDialogProps) {
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-h-[90svh] overflow-y-auto sm:max-w-xl">
        <DialogHeader>
          <DialogTitle>{departure ? `Edit ${formatDate(departure.startDate)}` : 'Add departure'}</DialogTitle>
          <DialogDescription>
            A dated trip of this {pkg.durationDays}-day package, with its own prices and seats.
          </DialogDescription>
        </DialogHeader>
        {/* Inside DialogContent = created fresh every time the dialog opens. */}
        <DepartureForm pkg={pkg} departure={departure} onDone={() => onOpenChange(false)} />
      </DialogContent>
    </Dialog>
  )
}

function DepartureForm({ pkg, departure, onDone }: { pkg: AdminPackage; departure?: AdminDeparture; onDone: () => void }) {
  const queryClient = useQueryClient()
  const [formError, setFormError] = useState<string | null>(null)

  const form = useForm<DepartureInput>({ resolver: zodResolver(departureSchema), defaultValues: toDepartureForm(departure) })
  const { errors, isSubmitting } = form.formState

  // Same rules as Departure.Update on the API: once seats are booked, the
  // date is fixed and the seat count can't drop below the booked seats.
  const booked = departure?.reservedSeats ?? 0
  const dateLocked = booked > 0

  // The end date the API will work out - shown so staff can check it.
  const startDate = useWatch({ control: form.control, name: 'startDate' })
  const endDate = startDate ? addDays(startDate, pkg.durationDays - 1) : null

  const onSubmit = form.handleSubmit(async (values) => {
    setFormError(null)
    try {
      const body = toDepartureRequest(values)
      if (departure) await packagesApi.updateDeparture(departure.id, body)
      else await packagesApi.addDeparture(pkg.id, body)
      // Refreshes this tab, the package's "from" price and its publish problems.
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      onDone()
      // The dialog closes, so a toast is the "it worked".
      notify.success(departure ? 'Departure saved' : 'Departure added', { description: formatDate(body.startDate) })
    } catch (error) {
      setFormError(
        applyServerErrors(form, error, {
          departure_date_taken: 'startDate',
          departure_date_in_past: 'startDate',
          departure_dates_locked: 'startDate',
          departure_seats_below_reserved: 'totalSeats',
        }),
      )
    }
  })

  return (
    <form onSubmit={onSubmit} noValidate className="grid gap-6">
      {formError && <FormAlert kind="error">{formError}</FormAlert>}

      <FieldGroup title="When">
        <div className="grid gap-5 sm:grid-cols-2">
          <TextField
            label="Start date"
            type="date"
            // The browser's own date picker: no extra library, and good on phones.
            min={departure?.startDate && departure.startDate < todayInBangladesh() ? undefined : todayInBangladesh()}
            // readOnly, not disabled: React Hook Form leaves a DISABLED field's
            // value out of the submitted data, and the date must still be sent.
            readOnly={dateLocked}
            className={cn(dateLocked && 'bg-muted text-ink-500')}
            error={errors.startDate?.message}
            hint={
              dateLocked
                ? `${booked} seat(s) booked - the date can't move.`
                : endDate
                  ? `Ends ${formatDate(endDate)} (${pkg.durationDays} days / ${pkg.durationNights} nights).`
                  : undefined
            }
            {...form.register('startDate')}
          />
          <TextField
            label="Booking closes (days before)"
            inputMode="numeric"
            className="nums"
            hint="Time to arrange transport and hotels."
            error={errors.bookingCutoffDays?.message}
            {...form.register('bookingCutoffDays')}
          />
        </div>
      </FieldGroup>

      {/* Short labels so the three boxes fit one row even on a phone; the group's title says what they are. */}
      <FieldGroup title="Price per person (৳)">
        <div className="grid grid-cols-3 gap-3 sm:gap-5">
          <TextField label="Adult" inputMode="decimal" className="nums" error={errors.adultPrice?.message} {...form.register('adultPrice')} />
          <TextField label="Child" inputMode="decimal" className="nums" error={errors.childPrice?.message} {...form.register('childPrice')} />
          <TextField label="Infant" inputMode="decimal" className="nums" error={errors.infantPrice?.message} {...form.register('infantPrice')} />
        </div>
      </FieldGroup>

      <FieldGroup title="Seats and rooms">
        <div className="grid gap-5 sm:grid-cols-2">
          <TextField
            label="Total seats"
            inputMode="numeric"
            className="nums"
            hint={booked > 0 ? `At least ${booked} (already booked).` : undefined}
            error={errors.totalSeats?.message}
            {...form.register('totalSeats')}
          />
          <TextField
            label="Single room extra (৳)"
            inputMode="decimal"
            className="nums"
            hint="Empty = not offered."
            error={errors.singleSupplement?.message}
            {...form.register('singleSupplement')}
          />
        </div>
      </FieldGroup>

      <DialogFooter>
        <Button type="button" variant="outline" onClick={onDone} disabled={isSubmitting}>
          Cancel
        </Button>
        <Button type="submit" disabled={isSubmitting}>
          {isSubmitting && <Spinner />}
          {departure ? 'Save changes' : 'Add departure'}
        </Button>
      </DialogFooter>
    </form>
  )
}

/** A titled set of fields. role="group" + the title: screen readers announce "Price per person" before "Adult". */
function FieldGroup({ title, children }: { title: string; children: ReactNode }) {
  const id = useId()
  return (
    <div role="group" aria-labelledby={id} className="grid gap-3">
      <h3 id={id} className="text-[0.6875rem] font-semibold tracking-wider text-ink-400 uppercase">
        {title}
      </h3>
      {children}
    </div>
  )
}
