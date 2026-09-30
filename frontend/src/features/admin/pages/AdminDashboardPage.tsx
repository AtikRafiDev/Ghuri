import { Card, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuth } from '@/features/auth/useAuth'

// The cards the Day 12 dashboard will fill with real numbers (14-day plan:
// "today's bookings, revenue, pending payments, upcoming departures").
const upcomingCards = ["Today's bookings", 'Revenue', 'Pending payments', 'Upcoming departures']

/** The (still empty) admin panel - Day 2's "done when": an admin can log in and see it. */
export function AdminDashboardPage() {
  const { user } = useAuth()

  return (
    <div className="grid gap-6">
      <div>
        <h1 className="text-2xl font-semibold">Dashboard</h1>
        <p className="text-muted-foreground">Welcome, {user?.fullName}. Manage destinations and categories from the Catalogue menu.</p>
      </div>
      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {upcomingCards.map((title) => (
          <Card key={title}>
            <CardHeader>
              <CardDescription>{title}</CardDescription>
              <CardTitle className="text-2xl text-muted-foreground">-</CardTitle>
            </CardHeader>
          </Card>
        ))}
      </div>
    </div>
  )
}
