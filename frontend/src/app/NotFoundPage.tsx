import { ArrowLeftIcon, LuggageIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'

/**
 * Any address the site doesn't have. A "lost luggage" moment instead of a
 * bare error: a big 4-suitcase-4, a friendly line, and two ways back.
 */
export function NotFoundPage() {
  useDocumentMeta({ title: 'Page not found' })

  return (
    <section className="relative isolate grid justify-items-center gap-8 py-6 text-center sm:py-12">
      {/* A soft green halo behind the "404". */}
      <div aria-hidden className="absolute top-0 left-1/2 -z-10 size-[30rem] -translate-x-1/2 bg-[radial-gradient(closest-side,var(--color-forest-100),transparent)]" />

      {/* The "0" is a suitcase that wandered off - the number is said once, for screen readers, below. */}
      <div aria-hidden className="flex items-center gap-1 sm:gap-3">
        <BigFour />
        <LostSuitcase className="h-32 animate-float sm:h-48" />
        <BigFour />
      </div>

      <div className="grid max-w-xl justify-items-center gap-4">
        <span className="inline-flex h-7 items-center gap-2 rounded-full bg-sun-100 px-3 text-xs font-semibold text-sun-700 ring-1 ring-sun-300/50 ring-inset">
          <span className="size-1.5 rounded-full bg-sun-500" />
          Error 404 · Page not found
        </span>
        <h1 className="text-3xl font-bold text-ink-900 sm:text-4xl">This page went on a trip without us</h1>
        <p className="text-ink-500 sm:text-lg">
          We searched every bag on the belt. The link may be old, or the page may have moved - let’s get you back on the road.
        </p>
      </div>

      <div className="flex flex-wrap justify-center gap-3">
        <Button asChild size="lg">
          <Link to="/">
            <ArrowLeftIcon className="group-hover/button:-translate-x-0.5" />
            Back to the home page
          </Link>
        </Button>
        <Button asChild size="lg" variant="outline">
          <Link to="/packages">
            <LuggageIcon />
            Browse packages
          </Link>
        </Button>
      </div>
    </section>
  )
}

function BigFour() {
  return (
    <span className="bg-gradient-to-b from-forest-600 to-forest-900 bg-clip-text text-[7.5rem] leading-none font-extrabold tracking-tighter text-transparent sm:text-[11rem]">
      4
    </span>
  )
}

/**
 * A little suitcase drawn in the palette: forest body, sun strap and buckle,
 * a terracotta sticker, wheels, and a luggage tag with a "?" (or whatever
 * `mark` says). Decorative - always aria-hidden. Also used by RouteErrorPage.
 */
export function LostSuitcase({ className, mark = '?' }: { className?: string; mark?: string }) {
  return (
    <svg viewBox="0 0 160 190" aria-hidden className={cn('brand-surface w-auto shrink-0 overflow-visible', className)}>
      {/* Shadow on the floor. */}
      <ellipse cx="80" cy="182" rx="54" ry="6" className="fill-forest-950/10" />
      {/* Handle. */}
      <path d="M58 40V28a9 9 0 0 1 9-9h26a9 9 0 0 1 9 9v12" fill="none" strokeWidth="8" strokeLinecap="round" className="stroke-forest-900" />
      {/* Wheels. */}
      <circle cx="44" cy="168" r="8" className="fill-ink-700" />
      <circle cx="116" cy="168" r="8" className="fill-ink-700" />
      {/* Body, with a lighter top edge and two ribs. */}
      <rect x="22" y="38" width="116" height="126" rx="20" className="fill-forest-700" />
      <rect x="32" y="46" width="96" height="6" rx="3" className="fill-white/15" />
      <rect x="50" y="38" width="8" height="126" className="fill-forest-800/70" />
      <rect x="102" y="38" width="8" height="126" className="fill-forest-800/70" />
      {/* Strap and buckle. */}
      <rect x="22" y="96" width="116" height="14" className="fill-sun-500" />
      <rect x="70" y="91" width="20" height="24" rx="4" strokeWidth="3" className="fill-sun-300 stroke-sun-700" />
      {/* Stickers. */}
      <circle cx="80" cy="70" r="13" className="fill-clay-500" />
      <path d="M71 76l6-8 3.5 4 2.5-3 6 7z" className="fill-white/90" />
      <rect x="66" y="128" width="30" height="18" rx="5" transform="rotate(-8 81 137)" className="fill-forest-200" />
      {/* The luggage tag on its string, hanging off the handle. */}
      <path d="M98 22c14 2 26 10 30 24" fill="none" strokeWidth="2" className="stroke-ink-400" />
      <g transform="rotate(14 134 60)">
        <rect x="116" y="44" width="38" height="28" rx="6" strokeWidth="2.5" className="fill-sun-100 stroke-sun-500" />
        <circle cx="124" cy="58" r="3" className="fill-white stroke-sun-500" strokeWidth="2" />
        <text x="142" y="65" textAnchor="middle" className="fill-sun-700 text-[20px] font-extrabold">
          {mark}
        </text>
      </g>
    </svg>
  )
}
