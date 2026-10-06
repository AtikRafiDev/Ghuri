import { z } from 'zod'
import { isBangladeshiMobile } from '@/shared/lib/phone'

// The SAME rules as the API's CreateStaffUserValidator.
export const staffSchema = z.object({
  fullName: z.string().trim().min(1, 'Enter their name.').max(150, 'At most 150 characters.'),
  phone: z.string().trim().refine(isBangladeshiMobile, 'Enter a Bangladeshi mobile number, e.g. 01711000000.'),
  email: z
    .string()
    .trim()
    .min(1, 'Staff need an email - the welcome link is sent there.')
    .max(256, 'At most 256 characters.')
    .pipe(z.email('Enter a valid email address.')),
  role: z.union([z.literal(2), z.literal(3), z.literal(4)]),
})

export type StaffInput = z.infer<typeof staffSchema>
