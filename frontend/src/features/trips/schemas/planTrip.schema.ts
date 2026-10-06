import { z } from 'zod'
import { addDays, todayInBangladesh } from '@/shared/lib/dates'
import { tripLimits } from '../api/trips.api'

// The same rules the API checks (backend: SubmitCustomTripValidator +
// CustomTrip.Submit). Checked here too for instant feedback - the API
// stays the judge.

const count = (label: string, min: number) =>
  z.number({ error: `Enter the number of ${label}.` }).int().min(min, `At least ${min}.`).max(tripLimits.maxPeople)

export const planTripSchema = z
  .object({
    startDate: z.string().min(1, 'Choose the first day of your trip.'),
    adults: count('adults', 1),
    children: count('children', 0),
    infants: count('infants', 0),
    hotelLevel: z.union([z.literal(1), z.literal(2), z.literal(3)]),
    budgetPerPerson: z.number().positive('Enter an amount above zero, or leave it empty.').nullable(),
    notes: z.string().trim().max(2000, 'At most 2000 characters.'),
    legs: z
      .array(
        z.object({
          destinationId: z.string().min(1, 'Choose a destination.'),
          nights: z
            .number({ error: 'Enter the nights.' })
            .int()
            .min(1, 'At least 1 night.')
            .max(tripLimits.maxNightsPerLeg, `At most ${tripLimits.maxNightsPerLeg} nights.`),
          transferToNext: z.union([z.literal(1), z.literal(2), z.literal(3), z.literal(4), z.literal(5), z.literal(6)]),
        }),
      )
      .min(1, 'Add at least one destination.')
      .max(tripLimits.maxLegs, `At most ${tripLimits.maxLegs} destinations.`),
  })
  .superRefine((trip, ctx) => {
    const earliest = addDays(todayInBangladesh(), tripLimits.minLeadDays)
    if (trip.startDate && trip.startDate < earliest) {
      ctx.addIssue({ code: 'custom', path: ['startDate'], message: `We need ${tripLimits.minLeadDays} days to plan - start on ${earliest} or later.` })
    }
    if (trip.infants > trip.adults) {
      ctx.addIssue({ code: 'custom', path: ['infants'], message: 'Each infant needs an adult to travel with.' })
    }
    if (trip.adults + trip.children + trip.infants > tripLimits.maxPeople) {
      ctx.addIssue({ code: 'custom', path: ['adults'], message: `At most ${tripLimits.maxPeople} people - for bigger groups, please call us.` })
    }
    const nights = trip.legs.reduce((sum, leg) => sum + (Number.isFinite(leg.nights) ? leg.nights : 0), 0)
    if (nights > tripLimits.maxTotalNights) {
      ctx.addIssue({ code: 'custom', path: ['legs'], message: `A trip can be at most ${tripLimits.maxTotalNights} nights - you have ${nights}.` })
    }
  })

export type PlanTripInput = z.infer<typeof planTripSchema>
