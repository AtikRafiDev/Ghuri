import { useQuery } from '@tanstack/react-query'
import { InfoIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link, useSearchParams } from 'react-router'
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert'
import { Button } from '@/components/ui/button'
import { quoteQuery } from '@/features/catalog/api/catalog.api'
import { PriceBreakdown } from '@/features/catalog/components/PriceBreakdown'
import { readSelection } from '@/features/catalog/lib/bookingSelection'
import { toAppError } from '@/shared/api/problem'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { site } from '@/shared/config/site'
import { formatDate } from '@/shared/lib/dates'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'

/**
 * Checkout - FOR NOW only the trip summary, re-priced by the API from the
 * URL. Day 9 adds traveller details, the countdown and "Pay with SSLCommerz".
 * It's here so Book now leads somewhere real on staging.
 */
export function CheckoutPage() {
  useDocumentMeta({ title: 'Checkout' })
  const [search] = useSearchParams()
  const selection = readSelection(search)
  const quote = useQuery(quoteQuery(selection?.slug ?? '', selection?.params ?? null))

  if (!selection) {
    return (
      <Centered title="Nothing to book yet" text="Choose a package and a date first.">
        <Button asChild>
          <Link to="/packages">See packages</Link>
        </Button>
      </Centered>
    )
  }

  const backToPackage = (
    <Button asChild variant="outline">
      <Link to={`/packages/${encodeURIComponent(selection.slug)}`}>Back to the package</Link>
    </Button>
  )

  if (quote.isPending) return <PageSpinner />

  if (quote.isError) {
    // E.g. the seats went while the customer was logging in - the API's message says so.
    return (
      <Centered title="This trip can’t be booked as chosen" text={toAppError(quote.error).message}>
        {backToPackage}
      </Centered>
    )
  }

  const q = quote.data
  const people = [
    `${q.adults} adult${q.adults === 1 ? '' : 's'}`,
    q.children > 0 && `${q.children} child${q.children === 1 ? '' : 'ren'}`,
    q.infants > 0 && `${q.infants} infant${q.infants === 1 ? '' : 's'}`,
  ].filter(Boolean)

  return (
    <div className="mx-auto grid max-w-xl gap-6">
      <h1 className="text-2xl font-semibold tracking-tight">Checkout</h1>

      <section className="grid gap-4 rounded-xl border bg-card p-4">
        <div className="grid gap-1">
          <h2 className="font-semibold">{q.packageTitle}</h2>
          <p className="text-sm text-muted-foreground">
            {formatDate(q.startDate)} → {formatDate(q.endDate)} · {q.nights} night{q.nights === 1 ? '' : 's'}
          </p>
          <p className="text-sm text-muted-foreground">{people.join(', ')}</p>
        </div>
        <PriceBreakdown quote={q} />
      </section>

      <Alert>
        <InfoIcon />
        <AlertTitle>Online payment is coming soon</AlertTitle>
        <AlertDescription>To book this trip now, call us on {site.phone}.</AlertDescription>
      </Alert>

      <div>{backToPackage}</div>
    </div>
  )
}

function Centered({ title, text, children }: { title: string; text: string; children: ReactNode }) {
  return (
    <section className="grid justify-items-center gap-3 py-16 text-center">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="text-muted-foreground">{text}</p>
      <div className="mt-2">{children}</div>
    </section>
  )
}
