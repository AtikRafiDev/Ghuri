import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { myBookingsQuery } from '@/features/booking/api/bookings.api'
import { PageMessage } from '@/features/booking/components/PageMessage'
import { toAppError } from '@/shared/api/problem'
import { PageSpinner } from '@/shared/components/PageSpinner'
import { useDocumentMeta } from '@/shared/lib/useDocumentMeta'
import { BookingList } from '../components/BookingList'

/** /account/bookings - every booking of the logged-in customer, newest first (17-day plan, Day 11). */
export function MyBookingsPage() {
  useDocumentMeta({ title: 'My bookings' })
  const bookings = useQuery(myBookingsQuery)

  if (bookings.isPending) return <PageSpinner />

  if (bookings.isError) {
    return (
      <PageMessage title="Your bookings couldn't be loaded" text={toAppError(bookings.error).message}>
        <Button variant="outline" onClick={() => bookings.refetch()}>
          Try again
        </Button>
      </PageMessage>
    )
  }

  return (
    <div className="grid gap-6">
      <h1 className="text-2xl font-semibold">My bookings</h1>
      {bookings.data.length === 0 ? (
        <PageMessage title="No bookings yet" text="Your trips will show up here once you book one.">
          <Button asChild>
            <Link to="/packages">Find a trip</Link>
          </Button>
        </PageMessage>
      ) : (
        <BookingList bookings={bookings.data} />
      )}
    </div>
  )
}
