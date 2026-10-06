import { useQuery } from '@tanstack/react-query'
import { ArrowRightIcon, CompassIcon, LuggageIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { myBookingsQuery } from '@/features/booking/api/bookings.api'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { PageHeader } from '@/shared/components/PageHeader'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { BookingList } from '../components/BookingList'

/** /account/bookings - every booking of the logged-in customer, newest first (17-day plan, Day 11). */
export function MyBookingsPage() {
  useDocumentMeta({ title: 'My bookings' })
  const bookings = useQuery(myBookingsQuery)

  if (bookings.isPending) return <PageSpinner />

  const count = bookings.data?.length ?? 0

  return (
    <div className="grid gap-6">
      <PageHeader
        title="My bookings"
        description={count > 0 ? `${count} booking${count === 1 ? '' : 's'}, newest first - open one for its documents or to cancel.` : 'Every trip you book shows up here.'}
        actions={
          count > 0 && (
            <Button asChild variant="outline">
              <Link to="/packages">
                <CompassIcon />
                Browse packages
              </Link>
            </Button>
          )
        }
      />

      {bookings.isError ? (
        <div className="grid justify-items-start gap-3">
          <FormAlert kind="error">Your bookings couldn't be loaded. {toAppError(bookings.error).message}</FormAlert>
          <Button variant="outline" size="sm" onClick={() => bookings.refetch()}>
            Try again
          </Button>
        </div>
      ) : count === 0 ? (
        <EmptyState icon={LuggageIcon} title="No bookings yet" text="Your trips will show up here once you book one.">
          <Button asChild>
            <Link to="/packages">
              Browse packages
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </EmptyState>
      ) : (
        <BookingList bookings={bookings.data} />
      )}
    </div>
  )
}
