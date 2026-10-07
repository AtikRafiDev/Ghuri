import { MailIcon, MessageCircleIcon, PhoneIcon, TriangleAlertIcon, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { PageHero } from '@/shared/components/PageHero'
import { site, whatsAppLink } from '@/shared/config/site'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { Reveal } from '@/shared/motion/Reveal'
import type { Photo } from '@/shared/photos/photos'

type StaticPageProps = {
  title: string
  /** The line under the title. */
  description: string
  /** "6 October 2026" - when the text last changed. */
  updated: string
  /** True until the client (and, for Terms/Privacy/Refunds, a lawyer) has approved the text. */
  draft?: boolean
  /** A symbol for the page, in a glass tile above the title (decoration only). */
  icon?: LucideIcon
  /** A photo behind the hero (shared/photos). Without one, the brand green with its contour lines. */
  photo?: Photo
  children: ReactNode
}

/**
 * How the plain JSX of a page is styled - h2 / p / ul / table / links - so
 * the page files stay as readable as a text document. Comfortable reading:
 * 15px text on a 28px line, in a column about 70 characters wide.
 */
const prose = [
  'grid gap-4 text-[0.9375rem] leading-7 text-ink-600',
  // Section headings: a hairline above and generous space, so each section starts clearly.
  '[&>h2]:mt-3 [&>h2]:border-t [&>h2]:border-ink-100 [&>h2]:pt-7 [&>h2]:text-xl [&>h2]:leading-snug [&>h2]:font-bold [&>h2]:text-ink-900',
  '[&_strong]:font-semibold [&_strong]:text-ink-900',
  // Links in the text (not the buttons, which carry data-slot): forest, with a soft underline that darkens on hover.
  '[&_a:not([data-slot])]:font-semibold [&_a:not([data-slot])]:text-forest-700 [&_a:not([data-slot])]:underline [&_a:not([data-slot])]:decoration-forest-300 [&_a:not([data-slot])]:underline-offset-4 [&_a:not([data-slot]):hover]:decoration-forest-700',
  // Lists: forest dots instead of browser bullets, each centred on the first line (28px line → dot at 11px).
  '[&>ul]:grid [&>ul]:gap-2.5 [&>ul>li]:relative [&>ul>li]:pl-6 [&>ul>li]:before:absolute [&>ul>li]:before:top-[0.6875rem] [&>ul>li]:before:left-1 [&>ul>li]:before:size-1.5 [&>ul>li]:before:rounded-full [&>ul>li]:before:bg-forest-500',
  // Tables: a rounded frame, a quiet header row, hairlines between rows; the last column (the answer) in bold.
  '[&_table]:w-full [&_table]:border-separate [&_table]:border-spacing-0 [&_table]:overflow-hidden [&_table]:rounded-2xl [&_table]:text-sm [&_table]:ring-1 [&_table]:ring-ink-200',
  '[&_th]:bg-ink-50 [&_th]:px-4 [&_th]:py-3 [&_th]:text-left [&_th]:text-[0.6875rem] [&_th]:font-semibold [&_th]:tracking-wider [&_th]:text-ink-500 [&_th]:uppercase',
  '[&_td]:border-t [&_td]:border-ink-200 [&_td]:px-4 [&_td]:py-3 [&_td]:align-top [&_td]:leading-6 [&_td:last-child]:font-semibold [&_td:last-child]:text-ink-900',
].join(' ')

/**
 * The frame of every information page - About, FAQ, Terms, Privacy, Refund
 * policy (17-day plan, Day 16): a hero in the home page's style (full-bleed
 * route) with the page's symbol, the "last updated" date, the title and its
 * one-line description; then the text in a white reading card that rises in,
 * and - beside it on a laptop - a small "still have questions?" card that
 * stays in view. The text is plain JSX in each page file.
 */
export function StaticPage({ title, description, updated, draft = false, icon: Icon, photo, children }: StaticPageProps) {
  useDocumentMeta({ title })

  return (
    <article>
      <PageHero
        photo={photo}
        top={
          Icon && (
            <span aria-hidden className="flex size-12 items-center justify-center rounded-2xl bg-white/10 ring-1 ring-white/15 backdrop-blur-md">
              <Icon className="size-6 text-sun-300" strokeWidth={1.75} />
            </span>
          )
        }
        eyebrow={`Last updated ${updated}`}
        title={title}
        text={description}
      />

      {/* The reading column starts on the same line as the hero's title above it. */}
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-12 sm:py-16 lg:grid-cols-[minmax(0,48rem)_1fr] lg:items-start lg:gap-12">
        <div className="grid gap-6">
          {draft && (
            <Alert className="border-sun-100 bg-sun-50 text-sun-700 *:data-[slot=alert-description]:text-sun-700">
              <TriangleAlertIcon />
              <AlertDescription>
                <strong>Draft</strong> - this text is a working draft and will be reviewed before the website goes live.
              </AlertDescription>
            </Alert>
          )}

          <Reveal y={32}>
            <div className="rounded-3xl bg-card px-6 py-8 shadow-card ring-1 ring-ink-200/80 sm:px-10 sm:py-10">
              <div className={prose}>{children}</div>
            </div>
          </Reveal>
        </div>

        <aside className="grid gap-4 rounded-3xl bg-forest-50 p-6 ring-1 ring-forest-100 lg:sticky lg:top-24">
          <div className="grid gap-1">
            <h2 className="text-lg font-bold text-ink-900">Still have questions?</h2>
            <p className="text-sm text-ink-500">We're happy to help - before you book, and during the trip.</p>
          </div>
          <div className="grid gap-2">
            <Button asChild>
              <a href={whatsAppLink()} target="_blank" rel="noreferrer">
                <MessageCircleIcon />
                Chat on WhatsApp
              </a>
            </Button>
            <Button asChild variant="outline">
              <a href={`tel:${site.phone.replace(/[^\d+]/g, '')}`}>
                <PhoneIcon />
                {site.phone}
              </a>
            </Button>
            <Button asChild variant="outline">
              <a href={`mailto:${site.email}`}>
                <MailIcon />
                {site.email}
              </a>
            </Button>
          </div>
        </aside>
      </div>
    </article>
  )
}
