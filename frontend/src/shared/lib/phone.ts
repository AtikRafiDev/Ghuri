/**
 * Same rule as the API's PhoneNumber value object: 01XXXXXXXXX or
 * 8801XXXXXXXXX (spaces/dashes allowed), 3rd digit = operator (3-9).
 * Used by every form with a mobile number (register, checkout).
 */
export function isBangladeshiMobile(value: string): boolean {
  const digits = value.replace(/\D/g, '')
  return /^01[3-9]\d{8}$/.test(digits) || /^8801[3-9]\d{8}$/.test(digits)
}
