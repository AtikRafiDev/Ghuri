import { useQuery } from '@tanstack/react-query'
import { ChevronRightIcon, PlusIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { myTripsQuery } from '../api/trips.api'
import { TripStatusBadge } from '../components/TripStatusBadge'

/** /account/trips - the customer's custom trip requests, newest first (17-day plan, Day 14). */
export function MyTripsPage() {
  useDocumentMeta({ title: 'My trips' })
  const trips = useQuery(myTripsQuery)

  if (trips.isPending) return <PageSpinner />
  if (trips.isError) {
    return (
      <PageMessage title="Your trips couldn't be loaded" text={toAppError(trips.error).message}>
        <Button variant="outline" onClick={() => trips.refetch()}>
          Try again
        </Button>
      </PageMessage>
    )
  }

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-semibold">My trips</h1>
        <Button asChild>
          <Link to="/plan-trip">
            <PlusIcon />
            Plan a trip
          </Link>
        </Button>
      </div>

      {trips.data.length === 0 ? (
        <PageMessage title="No custom trips yet" text="Tell us where you want to go - we plan it and send you a price." />
      ) : (
        <ul className="divide-y rounded-xl border bg-card">
          {trips.data.map((t) => (
            <li key={t.tripNo}>
              <Link to={`/account/trips/${encodeURIComponent(t.tripNo)}`} className="flex items-center gap-4 p-4 transition-colors hover:bg-muted/50">
                <div className="grid min-w-0 flex-1 gap-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <span className="truncate font-medium">{t.route}</span>
                    <TripStatusBadge status={t.status} />
                  </div>
                  <p className="text-sm text-muted-foreground">
                    {formatDate(t.startDate)} → {formatDate(t.endDate)} · {t.totalNights} nights · {t.people} traveller{t.people === 1 ? '' : 's'}
                    {t.status === 2 && t.quoteTotal !== null && <> · <strong className="text-foreground">{formatTaka(t.quoteTotal)}</strong></>}
                  </p>
                  <p className="font-mono text-xs text-muted-foreground">{t.tripNo}</p>
                </div>
                <ChevronRightIcon className="size-4 shrink-0 text-muted-foreground" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
