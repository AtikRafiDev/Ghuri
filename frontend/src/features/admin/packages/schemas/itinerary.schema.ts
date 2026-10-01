import { z } from 'zod'
import { maxItineraryDays, type AdminPackage, type ItineraryDayRequest } from '../api/packages.api'

// The SAME limits as the API's SavePackageItineraryValidator (and the
// catalog.ItineraryDays columns).
const daySchema = z.object({
  title: z.string().trim().min(1, 'Enter a title for the day.').max(200, 'At most 200 characters.'),
  description: z.string().trim().min(1, 'Describe the day.').max(4000, 'At most 4000 characters.'),
  // Three ticks instead of a free-text "B,L,D" box - no typos possible.
  breakfast: z.boolean(),
  lunch: z.boolean(),
  dinner: z.boolean(),
  accommodation: z.string().trim().max(150, 'At most 150 characters.'),
})

export const itinerarySchema = z.object({
  days: z.array(daySchema).max(maxItineraryDays, `At most ${maxItineraryDays} days.`),
})

export type ItineraryInput = z.infer<typeof itinerarySchema>
export type ItineraryDayInput = ItineraryInput['days'][number]

export const emptyDay: ItineraryDayInput = {
  title: '',
  description: '',
  breakfast: false,
  lunch: false,
  dinner: false,
  accommodation: '',
}

/** Saved days → form values. "B,L,D" is split back into the three ticks. */
export function toItineraryForm(pkg: AdminPackage): ItineraryInput {
  return {
    days: pkg.itineraryDays.map((day) => {
      const meals = (day.meals ?? '').split(',')
      return {
        title: day.title,
        description: day.description,
        breakfast: meals.includes('B'),
        lunch: meals.includes('L'),
        dinner: meals.includes('D'),
        accommodation: day.accommodation ?? '',
      }
    }),
  }
}

/** Form values → API body. The ticks become "B,L,D" (or null when no meal is included). */
export function toItineraryRequest(values: ItineraryInput): ItineraryDayRequest[] {
  return values.days.map((day) => {
    const meals = [day.breakfast && 'B', day.lunch && 'L', day.dinner && 'D'].filter(Boolean).join(',')
    return {
      title: day.title,
      description: day.description,
      meals: meals || null,
      accommodation: day.accommodation || null,
    }
  })
}
