import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card'
import { useAuth } from '@/features/auth/useAuth'

/** The customer's home. Bookings, invoices and profile editing arrive on Day 11. */
export function AccountPage() {
  const { user } = useAuth()
  if (!user) return null // RequireAuth guarantees a user; this only satisfies TypeScript

  return (
    <div className="grid gap-6">
      <h1 className="text-2xl font-semibold">My account</h1>
      <Card>
        <CardHeader>
          <CardTitle>{user.fullName}</CardTitle>
          <CardDescription>Your details</CardDescription>
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
      <Card>
        <CardHeader>
          <CardTitle>My bookings</CardTitle>
          <CardDescription>Your trips will appear here once booking opens.</CardDescription>
        </CardHeader>
      </Card>
    </div>
  )
}
