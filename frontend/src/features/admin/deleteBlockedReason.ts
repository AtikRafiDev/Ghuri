/**
 * The "why can't I delete this?" message for a destination or category that
 * tour packages still use (the API refuses the delete with a 409). Undefined
 * = nothing blocks the delete.
 */
export function usedByPackagesReason(packageCount: number): string | undefined {
  return packageCount > 0 ? `Used by ${packageCount} package(s) - can't delete` : undefined
}
