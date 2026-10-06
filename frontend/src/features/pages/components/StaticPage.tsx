import { TriangleAlertIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'

type StaticPageProps = {
  title: string
  /** For search engines and link previews. */
  description: string
  /** "6 October 2026" - when the text last changed. */
  updated: string
  /** True until the client (and, for Terms/Privacy/Refunds, a lawyer) has approved the text. */
  draft?: boolean
  children: ReactNode
}

/**
 * The frame of every information page - About, FAQ, Terms, Privacy, Refund
 * policy (17-day plan, Day 16). The text is plain JSX in each page file:
 * h2 / p / ul are styled here, so the pages stay readable to edit.
 */
export function StaticPage({ title, description, updated, draft = false, children }: StaticPageProps) {
  useDocumentMeta({ title, description })

  return (
    <article className="mx-auto grid max-w-3xl gap-6">
      <header className="grid gap-1">
        <h1 className="text-3xl font-semibold tracking-tight">{title}</h1>
        <p className="text-sm text-muted-foreground">Last updated: {updated}</p>
      </header>

      {draft && (
        <Alert>
          <TriangleAlertIcon />
          <AlertDescription>
            <strong>Draft</strong> - this text is a working draft and will be reviewed before the website goes live.
          </AlertDescription>
        </Alert>
      )}

      <div className="grid gap-4 leading-relaxed [&_a]:font-medium [&_a]:underline [&_a]:underline-offset-4 [&_h2]:mt-4 [&_h2]:text-xl [&_h2]:font-semibold [&_li]:mt-1 [&_table]:w-full [&_table]:text-sm [&_td]:border-b [&_td]:py-2 [&_th]:border-b [&_th]:py-2 [&_th]:text-left [&_ul]:list-disc [&_ul]:pl-6">
        {children}
      </div>
    </article>
  )
}
