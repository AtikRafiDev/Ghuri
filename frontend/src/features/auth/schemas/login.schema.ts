import { z } from 'zod'

export const loginSchema = z.object({
  phoneOrEmail: z.string().trim().min(1, 'Enter your phone number or email.'),
  // Only "not empty" - an existing password must work even if the rules
  // for NEW passwords change later (same as the API's LoginValidator).
  password: z.string().min(1, 'Enter your password.'),
})

export type LoginInput = z.infer<typeof loginSchema>
