import { z } from 'zod'

// The same limits the API checks (backend: UpdateProfileValidator).
export const profileSchema = z.object({
  fullName: z.string().trim().min(1, 'Enter your name.').max(150, 'At most 150 characters.'),
  email: z.union([z.literal(''), z.email('Enter a valid email address.').max(256)]),
  currentPassword: z.string(),
})

export type ProfileInput = z.infer<typeof profileSchema>
