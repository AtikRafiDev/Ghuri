import { z } from 'zod'

// The SAME rules the API checks (backend: PasswordRules.cs, 8-128 chars).
// Checking here too means instant feedback while typing - but the API
// stays the real judge: these can be bypassed, its validators can't.
export const newPassword = z
  .string()
  .min(8, 'At least 8 characters.')
  .max(128, 'At most 128 characters.')

export const forgotPasswordSchema = z.object({
  email: z.email('Enter a valid email address.'),
})
export type ForgotPasswordInput = z.infer<typeof forgotPasswordSchema>

export const resetPasswordSchema = z
  .object({
    newPassword,
    confirmPassword: z.string(),
  })
  .refine((values) => values.newPassword === values.confirmPassword, {
    message: 'The passwords do not match.',
    path: ['confirmPassword'],
  })
export type ResetPasswordInput = z.infer<typeof resetPasswordSchema>
