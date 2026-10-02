import type { FieldValues, Path, UseFormReturn } from 'react-hook-form'
import { toAppError } from '@/shared/api/problem'

/**
 * Puts a failed API call's errors where the user will see them
 * (blueprint 13.2: "ProblemDetails field errors map back onto inputs"):
 * - field errors ("Password must be at least 8 characters") go under
 *   their input, exactly like the form's own validation messages;
 * - a code listed in codeToField (e.g. phone_taken → phone) too;
 * - everything else (wrong password, too many attempts, server down) is
 *   RETURNED, to be shown as one message above the form.
 */
export function applyServerErrors<T extends FieldValues>(
  form: UseFormReturn<T>,
  error: unknown,
  codeToField: Record<string, Path<T>> = {},
): string | null {
  const appError = toAppError(error)
  const fieldNames = Object.keys(form.getValues())

  const codeField = codeToField[appError.code]
  if (codeField) {
    form.setError(codeField, { type: 'server', message: appError.message })
    return null
  }

  let shownOnAField = false
  for (const [field, message] of Object.entries(appError.fieldErrors)) {
    // "travellers.1.fullName" belongs to the form's "travellers" list.
    if (fieldNames.includes(field.split('.')[0])) {
      form.setError(field as Path<T>, { type: 'server', message })
      shownOnAField = true
    }
  }
  return shownOnAField ? null : appError.message
}
