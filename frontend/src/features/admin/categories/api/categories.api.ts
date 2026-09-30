import { http } from '@/shared/api/http'

/** One row of GET /api/v1/admin/categories (backend: AdminCategoryDto). */
export type AdminCategory = {
  id: string
  name: string
  slug: string
  icon: string | null
  sortOrder: number
  /** Above 0 = the API refuses to delete it. */
  packageCount: number
}

/** Body of POST and PUT (backend: Create/UpdateCategoryCommand). */
export type CategoryRequest = { name: string; slug: string | null; icon: string | null; sortOrder: number }

const base = '/api/v1/admin/categories'

export const categoriesApi = {
  async list(): Promise<AdminCategory[]> {
    const { data } = await http.get<AdminCategory[]>(base)
    return data
  },

  async create(body: CategoryRequest): Promise<string> {
    const { data } = await http.post<{ id: string }>(base, body)
    return data.id
  },

  async update(id: string, body: CategoryRequest): Promise<void> {
    await http.put(`${base}/${id}`, body)
  },

  async remove(id: string): Promise<void> {
    await http.delete(`${base}/${id}`)
  },
}

export const categoryKeys = {
  all: ['admin', 'categories'] as const,
}
