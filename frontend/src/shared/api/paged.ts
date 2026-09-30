/** One page of a long list - the API's list shape (backend: Application/Common/Paged.cs). */
export type Paged<T> = {
  items: T[]
  page: number
  pageSize: number
  totalCount: number
  totalPages: number
}
