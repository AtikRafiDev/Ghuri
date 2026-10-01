import { useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'
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
  const [message, setMessage] = useState<{ kind: 'error' | 'success'; text: string } | null>(null)

  // Same photos in the same order = nothing to save.
  const changed = images.map((i) => i.id).join() !== saved.map((i) => i.id).join()

  const save = async () => {
    setSaving(true)
    setMessage(null)
    try {
      await packagesApi.setImages(pkg.id, images.map((image) => image.id))
      // Refreshes the list's cover photo and the publish problems too.
      await queryClient.invalidateQueries({ queryKey: packageKeys.all })
      setMessage({ kind: 'success', text: 'Photos saved.' })
    } catch (error) {
      // e.g. 422 package_must_stay_publishable: a published package can't lose its last photo.
      setMessage({ kind: 'error', text: toAppError(error).message })
    } finally {
      setSaving(false)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>Photos</CardTitle>
        <CardDescription>
          The first photo is the cover on cards and lists. Hotel, room, food and sights sell a package best.
        </CardDescription>
      </CardHeader>
      <CardContent className="grid gap-4">
        <ImageGalleryUpload
          id="package-images"
          value={images}
          onChange={(next) => {
            setImages(next)
            setMessage(null)
          }}
          max={maxPackageImages}
          onBusyChange={setUploading}
        />

        {message && <FormAlert kind={message.kind}>{message.text}</FormAlert>}

        <div className="flex items-center justify-end gap-2">
          {changed && <span className="text-sm text-muted-foreground">Unsaved changes</span>}
          <Button type="button" variant="outline" disabled={!changed || saving || uploading} onClick={() => setImages(saved)}>
            Undo changes
          </Button>
          <Button type="button" disabled={!changed || saving || uploading} onClick={save}>
            {saving && <Spinner />}
            Save photos
          </Button>
        </div>
      </CardContent>
    </Card>
  )
}
