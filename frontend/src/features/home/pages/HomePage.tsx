import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { useAuth } from '@/features/auth/useAuth'

/** Placeholder until Day 6 (hero search, featured packages, destinations). */
export function HomePage() {
  const { status } = useAuth()

  return (
    <section className="grid gap-6 py-12 text-center">
      <h1 className="text-4xl font-semibold tracking-tight sm:text-5xl">Your next trip starts here</h1>
      <p className="mx-auto max-w-xl text-muted-foreground">
        Tours across Bangladesh and beyond - pick a date, book your seats and pay online. Search and packages are on
        their way.
      </p>
      {status === 'anonymous' && (
        <div className="flex justify-center gap-3">
          <Button asChild size="lg">
            <Link to="/register">Create an account</Link>
          </Button>
          <Button asChild size="lg" variant="outline">
            <Link to="/login">Log in</Link>
          </Button>
        </div>
      )}
    </section>
  )
}
