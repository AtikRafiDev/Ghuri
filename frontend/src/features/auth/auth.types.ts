// Mirrors the API's SystemRole enum (backend: Ghuri.Domain/Enums/SystemRole.cs).
export type Role = 'SuperAdmin' | 'Manager' | 'Sales' | 'Accounts' | 'Customer'

/** Everyone allowed into the admin panel - the same list as the API's "AdminArea" policy. */
export const staffRoles: readonly Role[] = ['SuperAdmin', 'Manager', 'Sales', 'Accounts']

/** Who may open Admin → Staff and create staff accounts - the same as the API's "ManageStaff" policy. */
export const staffManagerRoles: readonly Role[] = ['SuperAdmin']

// Which admin sections each role sees (decided 2026-10-08). Custom trips are
// open to every staff member (staffRoles); the rest, each the same list as an API policy:

/** Admin → Dashboard - the API's "ViewDashboard" policy. */
export const dashboardRoles: readonly Role[] = ['SuperAdmin']

/** Admin → Bookings, Payments, Refunds - the API's "ViewBookings" policy. */
export const bookingDeskRoles: readonly Role[] = ['SuperAdmin', 'Manager', 'Accounts']

/** Admin → Packages, Destinations, Categories - the API's "ManageCatalogue" policy. */
export const catalogueRoles: readonly Role[] = ['SuperAdmin', 'Manager', 'Sales']

/** Admin → System health. No API policy: /health is public (load balancers call it). */
export const systemHealthRoles: readonly Role[] = ['SuperAdmin', 'Manager']

/** GET /api/v1/auth/me (backend: MeDto). */
export type Me = {
  id: string
  fullName: string
  email: string | null
  phone: string
  emailConfirmed: boolean
  phoneConfirmed: boolean
  roles: Role[]
}

/** POST /api/v1/auth/register - only what the API expects ("confirmPassword" is form-only, never sent). */
export type RegisterRequest = { fullName: string; phone: string; email: string | null; password: string }

/** Where to land after logging in when no page was requested: staff → admin panel, customers → their account. */
export function homePathFor(user: Me): string {
  return user.roles.some((role) => staffRoles.includes(role)) ? '/admin' : '/account'
}
