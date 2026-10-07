import type { PackageSearchParams } from '@/features/catalog/api/catalog.api'

/**
 * "Popular packages": featured first, then the newest - the search's
 * "Recommended" order, first 8. Shared, so two sections asking for it make
 * ONE request (TanStack Query caches by these exact values).
 */
export const popularPackagesSearch: PackageSearchParams = { sort: 'Recommended', pageSize: 8 }
