import { z } from 'zod'
import { slugProblem } from '@/shared/lib/slug'

// The SAME limits as the API's CategoryFieldsValidator.
export const categorySchema = z
  .object({
    name: z.string().trim().min(1, 'Enter a name.').max(100, 'At most 100 characters.'),
    slug: z.string().trim(),
    /** "" = no icon. */
    icon: z.string(),
    sortOrder: z
      .number({ error: 'Enter a whole number.' })
      .int('Enter a whole number.')
      .min(0, '0 or more.')
      .max(100_000, 'At most 100000.'),
  })
  .superRefine((values, ctx) => {
    const problem = slugProblem(values.slug, values.name, 120)
    if (problem) ctx.addIssue({ code: 'custom', path: ['slug'], message: problem })
  })

export type CategoryInput = z.infer<typeof categorySchema>
