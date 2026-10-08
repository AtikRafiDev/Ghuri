import { http } from '@/shared/api/http'

/** The API's SystemRole numbers for staff (backend: Ghuri.Domain/Enums/SystemRole.cs). */
export type StaffRole = 1 | 2 | 3 | 4

/** The roles the Super Admin can give - never Super Admin itself (backend: User.AssignableStaffRoles). */
export const assignableStaffRoles = [2, 3, 4] as const satisfies readonly StaffRole[]

export const staffRoleLabels: Record<StaffRole, string> = { 1: 'Super Admin', 2: 'Manager', 3: 'Sales', 4: 'Accounts' }

/** What each role may do in the admin panel - the same split as the API's policies. */
export const staffRoleHints: Record<StaffRole, string> = {
  1: 'Everything, including the dashboard and staff accounts.',
  2: 'Everything except the dashboard and staff accounts.',
  3: 'Catalogue and custom trips: edit packages, quote trips. No bookings or money.',
  4: 'Bookings, payments, refunds, custom trips: record payments, complete refunds. No catalogue, cancelling or quoting.',
}

/** The API's UserStatus: 1 Active, 3 Disabled (2 Locked is unused). */
export type UserStatus = 1 | 2 | 3

/** One row of GET /api/v1/admin/staff (backend: StaffUserDto). */
export type StaffUser = {
  id: string
  fullName: string
  phone: string
  email: string | null
  role: StaffRole
  status: UserStatus
  /** False = they haven't used the welcome link yet. */
  passwordSet: boolean
  lastLoginUtc: string | null
  /** False for the Super Admin. */
  editable: boolean
}

/** Body of POST /api/v1/admin/staff (backend: CreateStaffUserCommand). */
export type CreateStaffRequest = { fullName: string; phone: string; email: string; role: StaffRole }

const base = '/api/v1/admin/staff'

export const staffApi = {
  async list(): Promise<StaffUser[]> {
    const { data } = await http.get<StaffUser[]>(base)
    return data
  },

  /** Creates the account; the API emails them a link to set their password. */
  async create(body: CreateStaffRequest): Promise<string> {
    const { data } = await http.post<{ id: string }>(base, body)
    return data.id
  },

  async changeRole(id: string, role: StaffRole): Promise<void> {
    await http.put(`${base}/${id}/role`, { role })
  },

  async disable(id: string): Promise<void> {
    await http.post(`${base}/${id}/disable`)
  },

  async enable(id: string): Promise<void> {
    await http.post(`${base}/${id}/enable`)
  },

  async sendPasswordLink(id: string): Promise<void> {
    await http.post(`${base}/${id}/password-link`)
  },
}

export const staffKeys = {
  all: ['admin', 'staff'] as const,
}
