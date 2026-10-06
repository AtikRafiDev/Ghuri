import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Spinner } from '@/components/ui/spinner'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
import { notify } from '@/shared/lib/notify'
import { ImageGalleryUpload, type UploadedImage } from '../../components/ImageGalleryUpload'
import { maxPackageImages, packageKeys, packagesApi, type AdminPackage } from '../api/packages.api'

/**
 * The package's photo gallery. Photos upload as soon as they're picked
 * (POST /api/v1/files), but the GALLERY only changes on "Save photos"
 * (PUT .../images) - so the admin can re-order freely and save once.
 */
export function PackagePhotosTab({ pkg }: { pkg: AdminPackage }) {
  const queryClient = useQueryClient()
  const saved = pkg.images.map((image) => ({ id: image.fileId, url: image.url }))
  const [images, setImages] = useState<UploadedImage[]>(saved)
  const [uploading, setUploading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState<string | null>(null)

  // Same photos in the same order = nothing to save.
  const changed = images.map((i) => i.id).join() !== saved.map((i) => i.id).join()

  const save = async () => {
    setSaving(true)
    setError(null)
    try {
      await packagesApi.setImages(pkg.id, images.map((image) => image.id))
      // Refreshes the list's cover photo and the publish problems too.
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      notify.success('Photos saved', { description: `${images.length} ${images.length === 1 ? 'photo' : 'photos'} in the gallery` })
    } catch (e) {
      // e.g. 422 package_must_stay_publishable: a published package can't lose its last photo.
      setError(toAppError(e).message)
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="grid gap-6">
      <section className="grid gap-5 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80 sm:p-6">
        <header className="grid gap-0.5">
          <h2 className="text-base font-bold text-ink-900">Photos</h2>
          <p className="text-sm text-ink-500">
            The first photo is the cover on cards and lists. Hotel, room, food and sights sell a package best.
          </p>
        </header>

        <ImageGalleryUpload
          id="package-images"
          value={images}
          onChange={(next) => {
            setImages(next)
            setError(null)
          }}
          max={maxPackageImages}
          onBusyChange={setUploading}
        />

        {error && <FormAlert kind="error">{error}</FormAlert>}
      </section>

      {/* The same floating Save bar as the other tabs. */}
      <div className="sticky bottom-4 z-10 flex flex-wrap items-center justify-end gap-2 justify-self-end rounded-2xl bg-card/90 p-2 shadow-pop ring-1 ring-ink-200/80 backdrop-blur-md">
        {changed && (
          <span className="flex items-center gap-2 px-2 text-sm font-medium text-sun-700">
            <span aria-hidden className="size-2 rounded-full bg-sun-500" />
            Unsaved changes
          </span>
        )}
        <Button type="button" variant="outline" disabled={!changed || saving || uploading} onClick={() => setImages(saved)}>
          Undo changes
        </Button>
        <Button type="button" disabled={!changed || saving || uploading} onClick={save}>
          {saving && <Spinner />}
          Save photos
        </Button>
      </div>
    </div>
  )
}
