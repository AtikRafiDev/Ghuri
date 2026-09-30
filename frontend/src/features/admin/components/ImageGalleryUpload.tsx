import { ChevronLeftIcon, ChevronRightIcon, ImagePlusIcon, StarIcon, XIcon } from 'lucide-react'
import { useRef, useState, type DragEvent } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { acceptedImageTypes, filesApi, maxUploadBytes } from '../api/files.api'

/** What a form stores per photo: the id to save, the URL to show. */
export type UploadedImage = { id: string; url: string }

type Uploading = { key: string; name: string; progress: number }

type ImageGalleryUploadProps = {
  id: string
  /** In display order - the first is the cover. */
  value: UploadedImage[]
  onChange: (images: UploadedImage[]) => void
  max: number
  /** Tells the form uploads are running, so Save can wait for them. */
  onBusyChange?: (busy: boolean) => void
  invalid?: boolean
}

/**
 * A photo gallery field: pick or drop several photos -> each uploads
 * straight away (POST /api/v1/files) -> the form gets their ids. Photos can
 * be re-ordered, made the cover (= moved first) or removed. Saving the form
 * only sends the ids, in order.
 */
export function ImageGalleryUpload({ id, value, onChange, max, onBusyChange, invalid }: ImageGalleryUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState<Uploading[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [dragOver, setDragOver] = useState(false)
  const busy = uploading.length > 0
  const freeSlots = max - value.length

  const addFiles = async (files: File[]) => {
    // A second batch while one is running would start from the same old
    // `value` and, when it finished, overwrite the first batch's photos.
    if (busy || freeSlots <= 0) return

    const rejected: string[] = []
    const accepted: File[] = []
    for (const file of files) {
      if (!acceptedImageTypes.includes(file.type)) rejected.push(`${file.name}: not a JPEG, PNG or WebP image.`)
      else if (file.size > maxUploadBytes) rejected.push(`${file.name}: larger than 10 MB.`)
      else if (accepted.length >= freeSlots) rejected.push(`${file.name}: skipped - at most ${max} photos.`)
      else accepted.push(file)
    }
    setErrors(rejected)
    if (accepted.length === 0) return

    // All upload at the same time; each shows its own progress.
    const jobs = accepted.map((file, i) => ({ key: `${Date.now()}-${i}`, file }))
    setUploading(jobs.map((job) => ({ key: job.key, name: job.file.name, progress: 0 })))
    onBusyChange?.(true)

    const results = await Promise.allSettled(
      jobs.map((job) =>
        filesApi.uploadImage(job.file, (progress) =>
          setUploading((current) => current.map((u) => (u.key === job.key ? { ...u, progress } : u))),
        ),
      ),
    )

    const uploaded: UploadedImage[] = []
    const failed: string[] = []
    results.forEach((result, i) => {
      if (result.status === 'fulfilled') uploaded.push({ id: result.value.id, url: result.value.url })
      else {
        const appError = toAppError(result.reason)
        failed.push(`${jobs[i].file.name}: ${appError.fieldErrors.file ?? appError.message}`)
      }
    })
    // One update, in the order the files were chosen. Safe to use `value`
    // from when the upload started: every control that could change it -
    // including the file input itself - is disabled meanwhile.
    onChange([...value, ...uploaded])
    // A NEW array: React skips an update when it's handed the same one again.
    setErrors([...rejected, ...failed])
    setUploading([])
    onBusyChange?.(false)
    if (inputRef.current) inputRef.current.value = '' // choosing the same file again still triggers a change
  }

  const move = (from: number, to: number) => {
    const next = [...value]
    const [photo] = next.splice(from, 1)
    next.splice(to, 0, photo)
    onChange(next)
  }

  const onDrop = (event: DragEvent) => {
    event.preventDefault()
    setDragOver(false)
    void addFiles(Array.from(event.dataTransfer.files))
  }

  return (
    <div
      className={cn('grid gap-2 rounded-lg', dragOver && 'ring-2 ring-primary ring-offset-2')}
      onDragOver={(event) => {
        event.preventDefault()
        setDragOver(true)
      }}
      onDragLeave={() => setDragOver(false)}
      onDrop={onDrop}
    >
      <input
        ref={inputRef}
        id={id}
        type="file"
        multiple
        accept={acceptedImageTypes.join(',')}
        className="sr-only"
        // Not only the "Add photos" button: the "Photos" label and keyboard
        // focus also reach this input, so it must be switched off itself.
        disabled={busy || freeSlots <= 0}
        onChange={(event) => void addFiles(Array.from(event.target.files ?? []))}
      />

      <ul className="grid grid-cols-2 gap-2 sm:grid-cols-3 md:grid-cols-4">
        {value.map((photo, index) => (
          <li key={photo.id} className="overflow-hidden rounded-lg border bg-muted/40">
            <div className="relative">
              <img src={photo.url} alt="" className="aspect-video w-full object-cover" />
              {index === 0 && <Badge className="absolute top-1 left-1">Cover</Badge>}
            </div>
            <div className="flex items-center justify-between p-1">
              <div className="flex">
                <Button type="button" variant="ghost" size="icon-xs" aria-label="Move left" disabled={busy || index === 0} onClick={() => move(index, index - 1)}>
                  <ChevronLeftIcon />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-xs"
                  aria-label="Move right"
                  disabled={busy || index === value.length - 1}
                  onClick={() => move(index, index + 1)}
                >
                  <ChevronRightIcon />
                </Button>
              </div>
              <div className="flex">
                {index > 0 && (
                  <Button type="button" variant="ghost" size="icon-xs" aria-label="Make cover" title="Make cover" disabled={busy} onClick={() => move(index, 0)}>
                    <StarIcon />
                  </Button>
                )}
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-xs"
                  aria-label="Remove photo"
                  title="Remove"
                  disabled={busy}
                  onClick={() => onChange(value.filter((p) => p.id !== photo.id))}
                >
                  <XIcon />
                </Button>
              </div>
            </div>
          </li>
        ))}

        {uploading.map((u) => (
          <li key={u.key} className="flex aspect-video flex-col items-center justify-center gap-1 rounded-lg border bg-muted/40 p-2 text-xs text-muted-foreground">
            <Spinner />
            <span>{u.progress}%</span>
            <span className="w-full truncate text-center">{u.name}</span>
          </li>
        ))}

        {freeSlots > 0 && !busy && (
          <li>
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              aria-invalid={invalid || undefined}
              className={cn(
                'flex aspect-video w-full flex-col items-center justify-center gap-1 rounded-lg border border-dashed text-xs text-muted-foreground hover:bg-muted',
                invalid && 'border-destructive',
              )}
            >
              <ImagePlusIcon className="size-5" />
              Add photos
            </button>
          </li>
        )}
      </ul>

      <p className="text-sm text-muted-foreground">
        {value.length} of {max} photos · the first one is the cover · drop files here, JPEG/PNG/WebP up to 10 MB each
      </p>

      {errors.length > 0 && (
        <ul role="alert" className="grid gap-0.5 text-sm text-destructive">
          {errors.map((message) => (
            <li key={message}>{message}</li>
          ))}
        </ul>
      )}
    </div>
  )
}
