import { z } from 'zod'
import { maxDepartureSeats, type AdminDeparture, type DepartureRequest } from '../api/packages.api'

// The SAME limits as the API's DepartureFieldsValidator and Departure itself.
// Numbers are kept as TEXT while typing (an emptied box is "", not 0) - see package.schema.ts.

const money = (label: string, { required, allowZero }: { required: boolean; allowZero: boolean }) =>
  z
    .string()
    .trim()
    .superRefine((text, ctx) => {
      if (text === '') {
        if (required) ctx.addIssue({ code: 'custom', message: `Enter the ${label}.` })
        return
      }
      const value = Number(text)
      if (Number.isNaN(value) || value < 0 || (!allowZero && value === 0)) {
        ctx.addIssue({ code: 'custom', message: allowZero ? 'Enter 0 or more.' : 'Enter more than 0.' })
      } else if (!/^\d+(\.\d{1,2})?$/.test(text)) {
        ctx.addIssue({ code: 'custom', message: 'At most 2 decimals.' })
      }
    })

const whole = (min: number, max: number, missing: string) =>
  z
    .string()
    .trim()
    .min(1, missing)
    .refine((text) => Number.isInteger(Number(text)) && Number(text) >= min && Number(text) <= max, {
      message: `Enter a whole number from ${min} to ${max}.`,
    })

export const departureSchema = z.object({
  // <input type="date"> gives "yyyy-MM-dd", or "" when empty.
  startDate: z.string().min(1, 'Choose the start date.'),
  adultPrice: money('adult price', { required: true, allowZero: false }),
  childPrice: money('child price (0 if free)', { required: true, allowZero: true }),
  infantPrice: money('infant price (0 if free)', { required: true, allowZero: true }),
  singleSupplement: money('single supplement', { required: false, allowZero: true }),
  totalSeats: whole(1, maxDepartureSeats, 'Enter the number of seats.'),
  bookingCutoffDays: whole(0, 60, 'Enter the number of days.'),
})

export type DepartureInput = z.infer<typeof departureSchema>

/** An existing departure (or nothing, for a new one) → the form's starting values. */
export function toDepartureForm(d?: AdminDeparture): DepartureInput {
  return {
    startDate: d?.startDate ?? '',
    adultPrice: d ? String(d.adultPrice) : '',
    childPrice: d ? String(d.childPrice) : '',
    infantPrice: d ? String(d.infantPrice) : '0',
    singleSupplement: d?.singleSupplement === null || d?.singleSupplement === undefined ? '' : String(d.singleSupplement),
    totalSeats: d ? String(d.totalSeats) : '',
    bookingCutoffDays: d ? String(d.bookingCutoffDays) : '2',
  }
}

export function toDepartureRequest(v: DepartureInput): DepartureRequest {
  return {
    startDate: v.startDate,
    adultPrice: Number(v.adultPrice),
    childPrice: Number(v.childPrice),
    infantPrice: Number(v.infantPrice),
    singleSupplement: v.singleSupplement === '' ? null : Number(v.singleSupplement),
    totalSeats: Number(v.totalSeats),
    bookingCutoffDays: Number(v.bookingCutoffDays),
  }
}
