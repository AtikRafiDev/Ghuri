import { CameraIcon } from 'lucide-react'
import { sitePhotos } from '@/shared/photos/photos'
import { StaticPage } from '../components/StaticPage'

/**
 * /photo-credits - the photographers behind the website's own photos (the
 * big pictures at the top of the pages), linked from the footer. Their
 * licences (CC BY-SA) let the site use the photos as long as each one is
 * credited with links - here, in one place, instead of on every photo.
 * The list is shared/photos' `sitePhotos`, so a new photo shows up by itself.
 */
export function PhotoCreditsPage() {
  return (
    <StaticPage
      title="Photo credits"
      description="The photographers behind the pictures on this website - thank you."
      updated="7 October 2026"
      icon={CameraIcon}
    >
      <p>
        The large photos at the top of our pages come from Wikimedia Commons. Their licences let us use them, resized, as long as we name each
        photographer and the licence - so here they are.
      </p>
      <div className="grid gap-6 sm:grid-cols-2">
        {sitePhotos.map(({ title, photo }) => (
          <figure key={title} className="grid content-start gap-3">
            <img
              src={photo.webp}
              alt={title}
              loading="lazy"
              decoding="async"
              className="aspect-[3/2] w-full rounded-2xl object-cover ring-1 ring-ink-200/80"
              style={{ objectPosition: photo.focus }}
            />
            <figcaption className="grid gap-0.5 text-sm leading-6">
              <span className="font-semibold text-ink-900">{title}</span>
              <span>
                Photo:{' '}
                <a href={photo.credit.source} target="_blank" rel="noopener noreferrer">
                  {photo.credit.author}
                </a>{' '}
                ·{' '}
                <a href={photo.credit.licenseUrl} target="_blank" rel="noopener noreferrer">
                  {photo.credit.license}
                </a>
              </span>
            </figcaption>
          </figure>
        ))}
      </div>
    </StaticPage>
  )
}
