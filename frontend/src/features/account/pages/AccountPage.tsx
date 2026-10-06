import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { Card, CardAction, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { Spinner } from '@/components/ui/spinner'
import { useAuth } from '@/features/auth/useAuth'
import { myBookingsQuery } from '@/features/booking/api/bookings.api'
import { toAppError } from '@/shared/api/problem'
import { BookingList } from '../components/BookingList'

/** How many bookings the overview shows; "See all" leads to the rest. */
const recentCount = 3

/** /account - the customer's home: their details and latest bookings. */
export function AccountPage() {
  const { user } = useAuth()
  const bookings = useQuery(myBookingsQuery)
  if (!user) return null // RequireAuth guarantees a user; this only satisfies TypeScript

  return (
    <div className="grid gap-6">
      <h1 className="text-2xl font-semibold">My account</h1>
      <Card>
        <CardHeader>
          <CardTitle>{user.fullName}</CardTitle>
          <CardDescription>Your details</CardDescription>
          <CardAction>
            <Button asChild variant="outline" size="sm">
              <Link to="/account/profile">Edit</Link>
            </Button>
          </CardAction>
        </CardHeader>
        <CardContent>
          <dl className="grid grid-cols-[max-content_1fr] gap-x-6 gap-y-2 text-sm">
            <dt className="text-muted-foreground">Mobile</dt>
            <dd>{user.phone}</dd>
            <dt className="text-muted-foreground">Email</dt>
            <dd>{user.email ?? '-'}</dd>
          </dl>
        </CardContent>
      </Card>

      <section className="grid gap-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-semibold">Latest bookings</h2>
          {bookings.data && bookings.data.length > recentCount && (
            <Link to="/account/bookings" className="text-sm font-medium underline underline-offset-4">
              See all {bookings.data.length}
            </Link>
          )}
        </div>
        {bookings.isPending && <Spinner />}
        {bookings.isError && <p className="text-sm text-destructive">{toAppError(bookings.error).message}</p>}
        {bookings.data?.length === 0 && (
          <p className="text-sm text-muted-foreground">
            No bookings yet.{' '}
            <Link to="/packages" className="font-medium text-foreground underline underline-offset-4">
              Find a trip
            </Link>
          </p>
        )}
        {bookings.data && bookings.data.length > 0 && <BookingList bookings={bookings.data.slice(0, recentCount)} />}
      </section>
    </div>
  )
}
