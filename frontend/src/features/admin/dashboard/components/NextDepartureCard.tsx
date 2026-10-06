import { ArrowRightIcon, BusIcon, CalendarClockIcon, UsersIcon } from 'lucide-react'
import { useEffect, useState } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import type { AdminDashboard } from '@/features/admin/operations/api/operations.api'
import { formatDate } from '@/shared/lib/dates'
import { dhakaMidnight } from '../lib/chartMath'

type Trip = AdminDashboard['upcomingTrips'][number]

/** The current time, ticking once a second while the card is on screen. */
function useNow() {
  const [now, setNow] = useState(() => Date.now())
  useEffect(() => {
    const timer = window.setInterval(() => setNow(Date.now()), 1000)
    return () => window.clearInterval(timer)
  }, [])
  return now
}

const two = (n: number) => String(n).padStart(2, '0')

/**
 * The dark card: a live countdown to the next confirmed departure (to the
 * start of its day, Dhaka time), so the team sees at a glance how close
 * the next group is. Trips are ordered soonest first by the API.
 */
export function NextDepartureCard({ trip }: { trip: Trip | undefined }) {
  const now = useNow()

  return (
    <section className="brand-surface relative isolate flex h-full min-h-64 flex-col justify-between gap-6 overflow-hidden rounded-3xl bg-gradient-to-br from-forest-800 to-forest-950 p-6 text-white shadow-lift">
      <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
      <div aria-hidden className="absolute -right-12 -bottom-16 -z-10 size-48 rounded-full bg-sun-500/20 blur-3xl" />

      <header className="flex items-center justify-between gap-3">
        <h2 className="flex items-center gap-2 text-sm font-semibold text-forest-100">
          <BusIcon className="size-4 text-sun-300" />
          Next departure
        </h2>
        {trip && (
          <span className="flex items-center gap-1.5 rounded-full bg-white/10 px-2.5 py-1 text-xs font-medium ring-1 ring-white/15">
            <span className="relative flex size-2">
              <span className="absolute inset-0 animate-[ring-ping_1.6s_ease-out_infinite] rounded-full bg-sun-300" />
              <span className="relative size-2 rounded-full bg-sun-300" />
            </span>
            Live
          </span>
        )}
      </header>

      {trip ? <Countdown trip={trip} now={now} /> : <p className="text-lg font-semibold text-forest-100/80">No departures in the next 7 days.</p>}

      {trip && (
        <Button asChild variant="accent" className="w-full">
          <Link to={`/admin/bookings/${encodeURIComponent(trip.bookingNo)}`}>
            Open booking {trip.bookingNo}
            <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
          </Link>
        </Button>
      )}
    </section>
  )
}

function Countdown({ trip, now }: { trip: Trip; now: number }) {
  const left = Math.max(0, dhakaMidnight(trip.startDate) - now)
  const days = Math.floor(left / 86_400_000)
  const hours = Math.floor(left / 3_600_000) % 24
  const minutes = Math.floor(left / 60_000) % 60
  const seconds = Math.floor(left / 1000) % 60

  return (
    <div className="grid gap-4">
      <div className="grid gap-1">
        <p className="line-clamp-1 text-lg font-bold">{trip.packageTitle ?? 'Custom trip'}</p>
        <p className="flex flex-wrap items-center gap-x-3 gap-y-1 text-xs text-forest-100/75">
          <span className="flex items-center gap-1.5">
            <CalendarClockIcon className="size-3.5" />
            {formatDate(trip.startDate)}
          </span>
          <span className="flex items-center gap-1.5">
            <UsersIcon className="size-3.5" />
            {trip.travellers} traveller{trip.travellers === 1 ? '' : 's'} · {trip.contactName}
          </span>
        </p>
      </div>
      {left === 0 ? (
        <p className="text-4xl font-bold tracking-tight text-sun-300">Departing today</p>
      ) : (
        // tabular figures here: the digits change every second and must not jiggle.
        <div className="nums flex items-end gap-2 text-4xl leading-none font-bold tracking-tight" role="timer" aria-label={`${days} days ${hours} hours ${minutes} minutes to go`}>
          {days > 0 && <Unit value={String(days)} label="days" />}
          <Unit value={two(hours)} label="hrs" />
          <span className="pb-4 text-forest-400">:</span>
          <Unit value={two(minutes)} label="min" />
          <span className="pb-4 text-forest-400">:</span>
          <Unit value={two(seconds)} label="sec" />
        </div>
      )}
    </div>
  )
}

function Unit({ value, label }: { value: string; label: string }) {
  return (
    <span className="grid justify-items-center gap-1.5">
      <span>{value}</span>
      <span className="text-[0.625rem] font-semibold tracking-wider text-forest-100/60 uppercase">{label}</span>
    </span>
  )
}
