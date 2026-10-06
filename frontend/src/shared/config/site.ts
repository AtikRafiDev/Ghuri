/**
 * The agency's public details, in ONE place: the header, footer, the policy
 * pages and the WhatsApp button all read them from here.
 *
 * ⚠ PLACEHOLDERS - replace with the client's real details before go-live.
 * Search the code for "site." to see where each one appears.
 */
export const site = {
  name: 'Ghuri',
  tagline: 'Tours across Bangladesh and beyond',
  phone: '+880 1XXX-XXXXXX',
  email: 'hello@example.com',
  address: 'Dhaka, Bangladesh',
  /**
   * The WhatsApp number in international form, digits only: 8801711000000
   * (880 = Bangladesh, then the mobile number without its first 0).
   * ⚠ PLACEHOLDER (decided 2026-10-06) - the button opens a chat with a
   * number that doesn't exist until this is replaced.
   */
  whatsApp: '8801000000000',
  /** The first message, already typed in the customer's WhatsApp. */
  whatsAppGreeting: 'Hello Ghuri, I have a question about a trip.',
} as const

/** "https://wa.me/8801711000000?text=…" - opens WhatsApp (app or web) with the greeting typed. */
export function whatsAppLink(message: string = site.whatsAppGreeting): string {
  return `https://wa.me/${site.whatsApp}?text=${encodeURIComponent(message)}`
}
