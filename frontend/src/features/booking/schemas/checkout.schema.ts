import { z } from 'zod'
import { isBangladeshiMobile } from '@/shared/lib/phone'

// The checkout form - the same rules as the API's CreateBookingValidator, so
// most mistakes are caught before anything is sent. The API checks again.

export const checkoutSchema = z.object({
  // One row per traveller; type and lead are fixed by the trip choice, only the name is typed.
  travellers: z.array(
    z.object({
      type: z.union([z.literal(1), z.literal(2), z.literal(3)]),
      isLead: z.boolean(),
      fullName: z.string().trim().min(1, "Enter the traveller's full name.").max(150, 'At most 150 characters.'),
    }),
  ),
  contactName: z.string().trim().min(1, "Enter the contact person's name.").max(150, 'At most 150 characters.'),
  contactPhone: z.string().trim().refine(isBangladeshiMobile, 'Enter a Bangladeshi mobile number, e.g. 01711000000.'),
  // Required: the payment receipt and the voucher are sent there.
  contactEmail: z.email('Enter a valid email address.').max(256),
  specialRequest: z.string().trim().max(1000, 'At most 1000 characters.'),
})

export type CheckoutInput = z.infer<typeof checkoutSchema>
