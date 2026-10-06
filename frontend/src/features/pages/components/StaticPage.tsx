import { CalendarClockIcon, TriangleAlertIcon, type LucideIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'

type StaticPageProps = {
  title: string
  /** For search engines and link previews - and the line under the title. */
  description: string
  /** "6 October 2026" - when the text last changed. */
  updated: string
  /** True until the client (and, for Terms/Privacy/Refunds, a lawyer) has approved the text. */
  draft?: boolean
  /** A symbol for the page, shown large in the header band on wide screens (decoration only). */
  icon?: LucideIcon
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
 * policy (17-day plan, Day 16): a green header band with the title, its
 * one-line description and the "last updated" date, then the text in a
 * white reading card. The text is plain JSX in each page file.
 */
export function StaticPage({ title, description, updated, draft = false, icon: Icon, children }: StaticPageProps) {
  useDocumentMeta({ title, description })

  return (
    <article className="grid gap-8 sm:gap-10">
      <header className="brand-surface relative isolate overflow-hidden rounded-[2rem] bg-gradient-to-br from-forest-700 via-forest-800 to-forest-950 py-12 text-white shadow-lift sm:py-16">
        <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
        <div aria-hidden className="absolute -top-24 -right-16 -z-10 size-80 rounded-full bg-sun-500/10 blur-3xl" />
        <div aria-hidden className="absolute -bottom-32 -left-20 -z-10 size-80 rounded-full bg-forest-400/20 blur-3xl" />
        {Icon && (
          <span
            aria-hidden
            className="absolute top-1/2 right-14 hidden size-28 -translate-y-1/2 rotate-6 animate-float items-center justify-center rounded-3xl bg-white/10 ring-1 ring-white/15 backdrop-blur-md xl:flex"
          >
            <Icon className="size-12 text-sun-300" strokeWidth={1.5} />
          </span>
        )}
        {/* Same width and inner padding as the reading card below, so the title starts exactly above the text. */}
        <div className="mx-auto grid max-w-3xl animate-fade-up gap-4 px-6 sm:px-10">
          <h1 className="text-3xl leading-tight font-bold sm:text-5xl">{title}</h1>
          <p className="max-w-2xl text-forest-100/80 sm:text-lg">{description}</p>
          <p className="inline-flex h-7 w-fit items-center gap-2 rounded-full bg-white/10 px-3 text-xs font-semibold text-forest-50 ring-1 ring-white/15">
            <CalendarClockIcon className="size-3.5 text-sun-300" />
            Last updated {updated}
          </p>
        </div>
      </header>

      <div className="mx-auto grid w-full max-w-3xl gap-6">
        {draft && (
          <Alert className="border-sun-100 bg-sun-50 text-sun-700 *:data-[slot=alert-description]:text-sun-700">
            <TriangleAlertIcon />
            <AlertDescription>
              <strong>Draft</strong> - this text is a working draft and will be reviewed before the website goes live.
            </AlertDescription>
          </Alert>
        )}

        <div className="animate-[fade-up_0.6s_var(--ease-out-expo)_0.08s_backwards] rounded-3xl bg-card px-6 py-8 shadow-card ring-1 ring-ink-200/80 sm:px-10 sm:py-10">
          <div className={prose}>{children}</div>
        </div>
      </div>
    </article>
  )
}
