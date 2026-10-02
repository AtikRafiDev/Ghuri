import { z } from 'zod'
import { isBangladeshiMobile } from '@/shared/lib/phone'
import { newPassword } from './password.schema'

export const registerSchema = z
  .object({
    fullName: z.string().trim().min(1, 'Enter your name.').max(150, 'At most 150 characters.'),
    phone: z.string().trim().refine(isBangladeshiMobile, 'Enter a Bangladeshi mobile number, e.g. 01711000000.'),
    // Optional for customers: empty is fine, otherwise it must be an email.
    email: z.union([z.literal(''), z.email('Enter a valid email address.').max(256)]),
    password: newPassword,
    confirmPassword: z.string(),
  })
  .refine((values) => values.password === values.confirmPassword, {
    message: 'The passwords do not match.',
    path: ['confirmPassword'],
  })

export type RegisterInput = z.infer<typeof registerSchema>
