import { useQuery } from '@tanstack/react-query'
import { AlertTriangleIcon, CalendarDaysIcon, ChevronRightIcon, MoonIcon, PlusIcon, RouteIcon, UsersIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { myTripsQuery } from '../api/trips.api'
import { TripStatusBadge } from '../components/TripStatusBadge'
import { RoutePath } from '../components/RoutePath'

/** /account/trips - the customer's custom trip requests, newest first (17-day plan, Day 14). */
export function MyTripsPage() {
  useDocumentMeta({ title: 'My trips' })
  const trips = useQuery(myTripsQuery)

  if (trips.isPending) return <PageSpinner />
  if (trips.isError) {
    return (
      <PageMessage icon={AlertTriangleIcon} tone="clay" title="Your trips couldn't be loaded" text={toAppError(trips.error).message}>
        <Button variant="outline" onClick={() => trips.refetch()}>
          Try again
        </Button>
      </PageMessage>
    )
  }

  const planButton = (
    <Button asChild>
      <Link to="/plan-trip">
        <PlusIcon />
        Plan a trip
      </Link>
    </Button>
  )

  return (
    <div className="grid gap-6">
      <PageHeader title="My trips" description="Custom trips you've asked us to plan - open one to see its quote and progress." actions={planButton} />

      {trips.data.length === 0 ? (
        <EmptyState icon={RouteIcon} title="No custom trips yet" text="Tell us where you want to go - we plan it and send you a price.">
          {planButton}
        </EmptyState>
      ) : (
        <ul className="stagger grid gap-3">
          {trips.data.map((t) => (
            <li key={t.tripNo}>
              <Link
                to={`/account/trips/${encodeURIComponent(t.tripNo)}`}
                className="group hover-lift flex items-center gap-4 rounded-2xl bg-card p-4 shadow-card ring-1 ring-ink-200/80 hover:ring-forest-200 sm:p-5"
              >
                <span className="hidden size-12 shrink-0 items-center justify-center rounded-2xl bg-forest-50 text-forest-600 sm:flex">
                  <RouteIcon className="size-5" />
                </span>

                <div className="grid min-w-0 flex-1 gap-2.5">
                  <div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-2">
                    <RoutePath stops={t.route.split(/\s*→\s*/)} />
                    <TripStatusBadge status={t.status} />
                  </div>
                  <div className="flex flex-wrap items-center gap-x-4 gap-y-1 text-sm text-ink-500">
                    <span className="flex items-center gap-1.5">
                      <CalendarDaysIcon className="size-4 text-ink-400" />
                      {formatDate(t.startDate)} → {formatDate(t.endDate)}
                    </span>
                    <span className="flex items-center gap-1.5">
                      <MoonIcon className="size-4 text-ink-400" />
                      {t.totalNights} nights
                    </span>
                    <span className="flex items-center gap-1.5">
                      <UsersIcon className="size-4 text-ink-400" />
                      {t.people} traveller{t.people === 1 ? '' : 's'}
                    </span>
                  </div>
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <span className="font-mono text-xs text-ink-400">{t.tripNo}</span>
                    {t.status === 2 && t.quoteTotal !== null && (
                      <span className="text-sm text-ink-500">
                        Quote <strong className="text-base font-bold text-forest-800">{formatTaka(t.quoteTotal)}</strong>
                      </span>
                    )}
                  </div>
                </div>

                <ChevronRightIcon className="size-5 shrink-0 text-ink-300 transition-[translate,color] duration-200 group-hover:translate-x-0.5 group-hover:text-forest-600" />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  )
}
