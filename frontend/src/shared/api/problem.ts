import { isAxiosError } from 'axios'

/** The JSON body of every API error: RFC 9457 ProblemDetails + our stable "code". */
type ProblemDetails = {
  title?: string
  status?: number
  code?: string
  errors?: Record<string, string[]>
}

/**
 * The ONE error type the UI deals with (blueprint 13.1: "ProblemDetails →
 * typed AppError"). Every failed API call - wrong password, validation,
 * server down - arrives as this, so every screen handles errors the same way.
 */
export class AppError extends Error {
  /** HTTP status; 0 = no answer at all (API down, offline). */
  readonly status: number
  /** Stable machine-readable reason from the API, e.g. "invalid_credentials". Switch on this, never on the message. */
  readonly code: string
  /** Per-field messages, keyed by the FORM's field name: { password: "..." }. */
  readonly fieldErrors: Record<string, string>

  constructor(status: number, code: string, message: string, fieldErrors: Record<string, string> = {}) {
    super(message)
    this.name = 'AppError'
    this.status = status
    this.code = code
    this.fieldErrors = fieldErrors
  }
}

export function toAppError(error: unknown): AppError {
  if (error instanceof AppError) return error

  if (isAxiosError(error)) {
    if (!error.response) {
      return new AppError(0, 'network_error', 'Cannot reach the server. Check your connection and try again.')
    }
    const problem = (error.response.data ?? {}) as ProblemDetails
    return new AppError(
      error.response.status,
      problem.code ?? `http_${error.response.status}`,
      problem.title ?? 'Something went wrong. Please try again.',
      toFieldErrors(problem.errors),
    )
  }

  return new AppError(0, 'unknown_error', 'Something went wrong. Please try again.')
}

// The API names fields the C# way ("Password", "NewPassword") or as a JSON
// path ("$.password"); our forms use camelCase ("password", "newPassword").
function toFieldErrors(errors: ProblemDetails['errors']): Record<string, string> {
  const result: Record<string, string> = {}
  for (const [key, messages] of Object.entries(errors ?? {})) {
    const name = key.split('.').pop() ?? key
    const field = name.charAt(0).toLowerCase() + name.slice(1)
    if (messages[0]) result[field] = messages[0]
  }
  return result
}
