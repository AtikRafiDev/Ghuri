import { http } from '@/shared/api/http'

/** POST /api/v1/files answer (backend: UploadedFileDto). */
export type UploadedFile = { id: string; url: string; width: number; height: number }

// The same limits the API enforces - checked here first so a wrong file is
// refused instantly, before it is sent over a slow connection.
export const maxUploadBytes = 10 * 1024 * 1024
export const acceptedImageTypes = ['image/jpeg', 'image/png', 'image/webp']

export const filesApi = {
  /** Uploads one image; the API stores it as WebP (max 1920 px) and returns its id + URL. */
  async uploadImage(file: File, onProgress?: (percent: number) => void): Promise<UploadedFile> {
    const body = new FormData()
    body.append('file', file) // "file" = the parameter name in FilesController.Upload
    const { data } = await http.post<UploadedFile>('/api/v1/files', body, {
      // A 10 MB photo on mobile data takes longer than the default 15 s.
      timeout: 120_000,
      onUploadProgress: (event) => {
        if (event.total) onProgress?.(Math.round((event.loaded / event.total) * 100))
      },
    })
    return data
  },
}
