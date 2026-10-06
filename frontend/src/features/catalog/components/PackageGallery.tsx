import { ChevronLeftIcon, ChevronRightIcon, ImageIcon, ImagesIcon } from 'lucide-react'
import { useState } from 'react'
import { cn } from '@/lib/utils'

// Both arrows share one look: a frosted white circle that turns solid on hover.
const arrowClass =
  'brand-surface absolute top-1/2 flex size-11 -translate-y-1/2 items-center justify-center rounded-full bg-white/80 text-ink-900 shadow-card ring-1 ring-white/60 backdrop-blur-md transition-[background-color,scale,color] duration-200 hover:scale-105 hover:bg-white hover:text-forest-800 focus-visible:ring-4 focus-visible:ring-ring/40 focus-visible:outline-none active:scale-95'

/** One big photo with previous/next, and a strip of thumbnails under it. The first photo is the cover. */
export function PackageGallery({ images, title }: { images: string[]; title: string }) {
  const [index, setIndex] = useState(0)

  if (images.length === 0) {
    return (
      <div className="flex aspect-[16/9] items-center justify-center rounded-3xl bg-ink-100 ring-1 ring-ink-200/80">
        <ImageIcon className="size-10 text-ink-300" />
      </div>
    )
  }

  const count = images.length
  const current = Math.min(index, count - 1)
  // Wraps around: "next" on the last photo goes back to the first.
  const step = (by: number) => setIndex((i) => (i + by + count) % count)

  return (
    <div className="grid gap-3">
      {/* isolate: the frosted buttons stay inside this frame's layer - WebKit could otherwise paint them over the sticky header. */}
      <div className="relative isolate aspect-[16/9] overflow-hidden rounded-3xl bg-ink-100 shadow-card ring-1 ring-ink-200/80">
        {/* key: a new photo mounts a new <img>, so each one fades in instead of snapping. */}
        <img
          key={images[current]}
          src={images[current]}
          alt={`${title} - photo ${current + 1} of ${count}`}
          fetchPriority={current === 0 ? 'high' : 'auto'}
          className="size-full animate-fade-in object-cover"
        />
        {count > 1 && (
          <>
            {/* A soft shade along the bottom so the counter reads on a bright photo. */}
            <div aria-hidden className="pointer-events-none absolute inset-x-0 bottom-0 h-24 bg-gradient-to-t from-forest-950/40 to-transparent" />
            <button type="button" className={cn(arrowClass, 'left-3 sm:left-4')} onClick={() => step(-1)} aria-label="Previous photo">
              <ChevronLeftIcon className="size-5" />
            </button>
            <button type="button" className={cn(arrowClass, 'right-3 sm:right-4')} onClick={() => step(1)} aria-label="Next photo">
              <ChevronRightIcon className="size-5" />
            </button>
            <span className="nums absolute right-3 bottom-3 inline-flex h-7 items-center gap-1.5 rounded-full bg-forest-950/60 px-3 text-xs font-semibold text-white ring-1 ring-white/15 backdrop-blur-md sm:right-4 sm:bottom-4">
              <ImagesIcon className="size-3.5" />
              {current + 1} / {count}
            </span>
          </>
        )}
      </div>

      {count > 1 && (
        // Scrolls sideways on a phone instead of wrapping into rows. The padding keeps the active ring from being clipped.
        <div className="-m-1 flex gap-2.5 overflow-x-auto p-1">
          {images.map((url, i) => (
            <button
              key={url}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`Show photo ${i + 1}`}
              aria-current={i === current}
              className={cn(
                'h-16 w-24 shrink-0 overflow-hidden rounded-xl ring-2 ring-transparent ring-offset-2 ring-offset-background transition-[opacity,box-shadow] duration-200 focus-visible:ring-ring focus-visible:outline-none',
                i === current ? 'opacity-100 ring-forest-600' : 'opacity-60 hover:opacity-100',
              )}
            >
              <img src={url} alt="" loading="lazy" className="size-full object-cover" />
            </button>
          ))}
        </div>
      )}
    </div>
  )
}
