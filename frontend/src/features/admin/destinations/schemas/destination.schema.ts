import { z } from 'zod'
import { slugProblem } from '@/shared/lib/slug'

// The SAME limits as the API's DestinationFieldsValidator (and the database
// columns) - instant feedback here, the API stays the real judge.
export const destinationSchema = z
  .object({
    name: z.string().trim().min(1, 'Enter a name.').max(150, 'At most 150 characters.'),
    slug: z.string().trim(),
    // A <select>'s value is always text; turned into a number when sending.
    countryId: z.string().min(1, 'Choose a country.'),
    summary: z.string().trim().max(500, 'At most 500 characters.'),
    // The gallery, in order - the first is the cover.
    images: z.array(z.object({ id: z.string(), url: z.string() })).max(10, 'At most 10 photos.'),
    isFeatured: z.boolean(),
    sortOrder: z
      .number({ error: 'Enter a whole number.' })
      .int('Enter a whole number.')
      .min(0, '0 or more.')
      .max(100_000, 'At most 100000.'),
    seoTitle: z.string().trim().max(70, 'At most 70 characters - Google cuts the rest.'),
    seoDescription: z.string().trim().max(160, 'At most 160 characters - Google cuts the rest.'),
  })
  .superRefine((values, ctx) => {
    const problem = slugProblem(values.slug, values.name, 160)
    if (problem) ctx.addIssue({ code: 'custom', path: ['slug'], message: problem })
  })

export type DestinationInput = z.infer<typeof destinationSchema>
