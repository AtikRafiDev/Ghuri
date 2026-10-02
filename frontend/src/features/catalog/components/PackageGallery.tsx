import { ChevronLeftIcon, ChevronRightIcon, ImageIcon } from 'lucide-react'
import { useState } from 'react'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'

/** One big photo with previous/next, and a strip of thumbnails under it. The first photo is the cover. */
export function PackageGallery({ images, title }: { images: string[]; title: string }) {
  const [index, setIndex] = useState(0)

  if (images.length === 0) {
    return (
      <div className="flex aspect-[16/9] items-center justify-center rounded-xl bg-muted">
        <ImageIcon className="size-10 text-muted-foreground" />
      </div>
    )
  }

  const count = images.length
  const current = Math.min(index, count - 1)
  // Wraps around: "next" on the last photo goes back to the first.
  const step = (by: number) => setIndex((i) => (i + by + count) % count)

  return (
    <div className="grid gap-2">
      <div className="relative aspect-[16/9] overflow-hidden rounded-xl bg-muted">
        <img
          src={images[current]}
          alt={`${title} - photo ${current + 1} of ${count}`}
          fetchPriority={current === 0 ? 'high' : 'auto'}
          className="size-full object-cover"
        />
        {count > 1 && (
          <>
            <Button
              variant="secondary"
              size="icon"
              className="absolute top-1/2 left-2 -translate-y-1/2 rounded-full opacity-90"
              onClick={() => step(-1)}
              aria-label="Previous photo"
            >
              <ChevronLeftIcon />
            </Button>
            <Button
              variant="secondary"
              size="icon"
              className="absolute top-1/2 right-2 -translate-y-1/2 rounded-full opacity-90"
              onClick={() => step(1)}
              aria-label="Next photo"
            >
              <ChevronRightIcon />
            </Button>
            <span className="absolute right-2 bottom-2 rounded-full bg-black/60 px-2 py-0.5 text-xs text-white tabular-nums">
              {current + 1} / {count}
            </span>
          </>
        )}
      </div>

      {count > 1 && (
        // Scrolls sideways on a phone instead of wrapping into rows.
        <div className="flex gap-2 overflow-x-auto pb-1">
          {images.map((url, i) => (
            <button
              key={url}
              type="button"
              onClick={() => setIndex(i)}
              aria-label={`Show photo ${i + 1}`}
              aria-current={i === current}
              className={cn(
                'h-14 w-20 shrink-0 overflow-hidden rounded-md opacity-70 transition-opacity hover:opacity-100 focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none',
                i === current && 'opacity-100 ring-2 ring-primary',
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
