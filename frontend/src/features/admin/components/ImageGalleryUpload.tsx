import { ChevronLeftIcon, ChevronRightIcon, CircleAlertIcon, CloudUploadIcon, StarIcon, XIcon } from 'lucide-react'
import { useRef, useState, type DragEvent } from 'react'
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

// The buttons laid over a photo: white on the dark fade, a soft glass circle.
const overlayButton = 'rounded-full bg-forest-950/40 text-white backdrop-blur-sm hover:bg-forest-950/70 hover:text-white'

/**
 * A photo gallery field: pick or drop several photos -> each uploads
 * straight away (POST /api/v1/files) -> the form gets their ids. Photos can
 * be re-ordered, made the cover (= moved first) or removed. Saving the form
 * only sends the ids, in order.
 *
 * Looks: a dashed green drop zone (big while the gallery is empty, a tile
 * after it), photo tiles whose buttons appear on hover or keyboard focus
 * (always shown on touch screens, which have no hover), and a progress tile
 * per file while it uploads.
 */
export function ImageGalleryUpload({ id, value, onChange, max, onBusyChange, invalid }: ImageGalleryUploadProps) {
  const inputRef = useRef<HTMLInputElement>(null)
  const [uploading, setUploading] = useState<Uploading[]>([])
  const [errors, setErrors] = useState<string[]>([])
  const [dragOver, setDragOver] = useState(false)
  const busy = uploading.length > 0
  const freeSlots = max - value.length
  const empty = value.length === 0 && !busy

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
      className={cn('grid gap-3 rounded-2xl transition-shadow duration-200', dragOver && 'ring-4 ring-forest-500/20 ring-offset-4 ring-offset-card')}
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

      <ul className="grid grid-cols-2 gap-3 sm:grid-cols-3 md:grid-cols-4">
        {value.map((photo, index) => (
          <li key={photo.id} className="group relative aspect-video overflow-hidden rounded-xl bg-ink-100 shadow-soft ring-1 ring-ink-200/80">
            <img src={photo.url} alt="" className="size-full object-cover transition-transform duration-500 group-hover:scale-105" />

            {index === 0 ? (
              <span className="absolute top-2 left-2 inline-flex h-6 items-center gap-1 rounded-full bg-primary px-2 text-[0.6875rem] font-semibold text-white shadow-soft">
                <StarIcon className="size-3 fill-current" aria-hidden />
                Cover
              </span>
            ) : (
              <span className="nums absolute top-2 left-2 inline-flex size-6 items-center justify-center rounded-full bg-forest-950/45 text-[0.6875rem] font-semibold text-white backdrop-blur-sm">
                {index + 1}
              </span>
            )}

            {/* Hidden until hover or keyboard focus; always visible on touch screens. */}
            <Button
              type="button"
              variant="ghost"
              size="icon-xs"
              aria-label={`Remove photo ${index + 1}`}
              title="Remove"
              disabled={busy}
              onClick={() => onChange(value.filter((p) => p.id !== photo.id))}
              className="absolute top-1.5 right-1.5 rounded-full bg-forest-950/40 text-white opacity-0 backdrop-blur-sm transition-[opacity,background-color] group-focus-within:opacity-100 group-hover:opacity-100 hover:bg-clay-600 hover:text-white [@media(hover:none)]:opacity-100"
            >
              <XIcon />
            </Button>
            <div className="absolute inset-x-0 bottom-0 flex items-center justify-between gap-1 bg-linear-to-t from-forest-950/70 to-transparent p-1.5 pt-6 opacity-0 transition-opacity duration-200 group-focus-within:opacity-100 group-hover:opacity-100 [@media(hover:none)]:opacity-100">
              <div className="flex gap-1">
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-xs"
                  aria-label={`Move photo ${index + 1} left`}
                  title="Move left"
                  className={overlayButton}
                  disabled={busy || index === 0}
                  onClick={() => move(index, index - 1)}
                >
                  <ChevronLeftIcon />
                </Button>
                <Button
                  type="button"
                  variant="ghost"
                  size="icon-xs"
                  aria-label={`Move photo ${index + 1} right`}
                  title="Move right"
                  className={overlayButton}
                  disabled={busy || index === value.length - 1}
                  onClick={() => move(index, index + 1)}
                >
                  <ChevronRightIcon />
                </Button>
              </div>
              {index > 0 && (
                <Button
                  type="button"
                  variant="ghost"
                  size="xs"
                  aria-label={`Make photo ${index + 1} the cover`}
                  className={cn(overlayButton, 'px-2.5 text-[0.6875rem]')}
                  disabled={busy}
                  onClick={() => move(index, 0)}
                >
                  <StarIcon />
                  Make cover
                </Button>
              )}
            </div>
          </li>
        ))}

        {uploading.map((u) => (
          <li
            key={u.key}
            className="relative flex aspect-video flex-col items-center justify-center gap-1.5 overflow-hidden rounded-xl bg-forest-50 p-3 text-xs text-forest-800 ring-1 ring-forest-100"
          >
            <Spinner className="size-5 text-forest-600" />
            <span className="nums font-semibold">{u.progress}%</span>
            <span className="w-full truncate text-center text-ink-500">{u.name}</span>
            <span aria-hidden className="absolute inset-x-0 bottom-0 h-1 bg-forest-100">
              <span className="block h-full bg-forest-500 transition-[width] duration-300" style={{ width: `${u.progress}%` }} />
            </span>
          </li>
        ))}

        {freeSlots > 0 && !busy && (
          <li className={cn(empty && 'col-span-full')}>
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              aria-invalid={invalid || undefined}
              className={cn(
                'group/drop flex w-full flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-forest-300 bg-forest-50/40 text-center text-forest-800 transition-[border-color,background-color,box-shadow] duration-200 outline-none hover:border-forest-500 hover:bg-forest-50 focus-visible:border-forest-500 focus-visible:ring-4 focus-visible:ring-forest-500/15',
                empty ? 'px-6 py-9' : 'aspect-video p-2',
                dragOver && 'border-forest-500 bg-forest-50',
                invalid && 'border-clay-500 bg-clay-50/50 hover:border-clay-500',
              )}
            >
              <span className="flex size-10 items-center justify-center rounded-full bg-card text-forest-600 shadow-soft ring-1 ring-forest-100 transition-transform duration-300 ease-(--ease-spring) group-hover/drop:-translate-y-0.5">
                <CloudUploadIcon className="size-5" />
              </span>
              <span className="text-sm font-semibold">{empty ? 'Add photos' : 'Add more'}</span>
              {empty && <span className="text-xs text-ink-500">Click to choose, or drop them here</span>}
            </button>
          </li>
        )}
      </ul>

      <p className="text-xs text-ink-500">
        <span className="nums font-semibold text-ink-700">
          {value.length} of {max}
        </span>{' '}
        photos · the first one is the cover · drop files here, JPEG/PNG/WebP up to 10 MB each
      </p>

      {errors.length > 0 && (
        <ul role="alert" className="grid gap-1 text-[0.8125rem] font-medium text-destructive">
          {errors.map((message) => (
            <li key={message} className="flex items-start gap-1.5">
              <CircleAlertIcon className="mt-px size-3.5 shrink-0" aria-hidden />
              {message}
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
