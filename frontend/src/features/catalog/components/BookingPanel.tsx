import { useQuery } from '@tanstack/react-query'
import { ArrowRightIcon, MinusIcon, PhoneIcon, PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { useNavigate } from 'react-router'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Separator } from '@/components/ui/separator'
import { Skeleton } from '@/components/ui/skeleton'
import { Spinner } from '@/components/ui/spinner'
import { useAuth } from '@/features/auth/useAuth'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { site } from '@/shared/config/site'
import { addDays, formatDate } from '@/shared/lib/dates'
import { formatTaka } from '@/shared/lib/format'
import {
  departuresQuery,
  quoteQuery,
  type AvailableDeparture,
  type PackageDetails,
  type QuoteParams,
} from '../api/catalog.api'
import { toSelectionSearch } from '../lib/bookingSelection'
import { PriceBreakdown } from './PriceBreakdown'

/** The API's limit (Travellers.MaxPerBooking): bigger groups ask for a custom trip. */
const maxTravellers = 20

type Travellers = { adults: number; children: number; infants: number }

/**
 * The "book this trip" box: pick a date (a departure, or a check-in date +
 * nights), say who's coming, see the live price, Book now.
 *
 * The price ALWAYS comes from the quote API - never added up here - so the
 * page, the checkout and the real booking can't disagree. This box only
 * blocks choices that are obviously wrong (before the earliest date, an
 * infant without an adult); every real rule is the API's.
 */
export function BookingPanel({ pkg }: { pkg: PackageDetails }) {
  const navigate = useNavigate()
  const { status } = useAuth()
  const isFlexible = pkg.pricingMode === 2

  const [travellers, setTravellers] = useState<Travellers>({ adults: 2, children: 0, infants: 0 })
  // Fixed: the date the customer clicked (null = not clicked yet).
  const [pickedDepartureId, setPickedDepartureId] = useState<string | null>(null)
  // Flexible: start on the earliest allowed day, for the shortest stay - so a price shows straight away.
  const [startDate, setStartDate] = useState(pkg.earliestStartDate ?? '')
  const [nights, setNights] = useState(pkg.minNights ?? 1)

  const departures = useQuery({ ...departuresQuery(pkg.slug), enabled: !isFlexible })
  const seats = travellers.adults + travellers.children // infants sit on a lap

  // Until a date is clicked, take the first one with enough seats - again, so a price shows straight away.
  const departure =
    departures.data?.find((d) => d.id === pickedDepartureId) ?? departures.data?.find((d) => d.seatsLeft >= seats)

  const earliest = pkg.earliestStartDate ?? ''
  const startDateOk = /^\d{4}-\d{2}-\d{2}$/.test(startDate) && startDate >= earliest // "yyyy-MM-dd" compares as text

  // What to price; null = not enough chosen yet, so no request is sent.
  const params: QuoteParams | null = isFlexible
    ? startDateOk
      ? { ...travellers, startDate, nights }
      : null
    : departure
      ? { ...travellers, departureId: departure.id }
      : null

  const quote = useQuery(quoteQuery(pkg.slug, params))
  const quoteError = quote.isError ? firstMessage(quote.error) : null
  // isPlaceholderData = the OLD price is still showing while the new one loads: don't book on it.
  const canBook = params !== null && quote.isSuccess && !quote.isPlaceholderData

  const onBook = () => {
    if (!params) return
    // /checkout requires a login; RequireAuth sends a visitor to /login and back here with the URL intact.
    navigate(`/checkout${toSelectionSearch({ slug: pkg.slug, params })}`)
  }

  return (
    <div className="grid gap-4 rounded-xl border bg-card p-4 shadow-sm">
      <h2 className="text-lg font-semibold">{isFlexible ? 'Choose your stay' : 'Choose a date'}</h2>

      {isFlexible ? (
        <FlexibleStayPicker
          pkg={pkg}
          startDate={startDate}
          onStartDateChange={setStartDate}
          nights={nights}
          onNightsChange={setNights}
          tooSoon={startDate !== '' && !startDateOk}
        />
      ) : (
        <DeparturePicker
          departures={departures.data}
          isPending={departures.isPending}
          isError={departures.isError}
          onRetry={() => departures.refetch()}
          selectedId={departure?.id ?? null}
          onSelect={setPickedDepartureId}
        />
      )}

      <Separator />
      <TravellerPicker value={travellers} onChange={setTravellers} />
      <Separator />

      <div className="grid gap-3" aria-live="polite">
        {quoteError ? (
          <p className="rounded-lg bg-destructive/10 p-3 text-sm text-destructive">{quoteError}</p>
        ) : params === null ? (
          <p className="text-sm text-muted-foreground">
            {isFlexible ? 'Pick a check-in date to see the price.' : 'Pick a date to see the price.'}
          </p>
        ) : quote.data ? (
          <div className={cn('transition-opacity', quote.isFetching && 'opacity-60')}>
            <PriceBreakdown quote={quote.data} />
          </div>
        ) : (
          <div className="grid gap-2">
            <Skeleton className="h-4 w-full" />
            <Skeleton className="h-4 w-2/3" />
            <Skeleton className="h-6 w-full" />
          </div>
        )}

        <Button size="lg" className="h-11 w-full text-base" disabled={!canBook} onClick={onBook}>
          {quote.isFetching && params !== null ? <Spinner /> : null}
          Book now
          <ArrowRightIcon />
        </Button>

        <p className="text-center text-xs text-muted-foreground">
          {status === 'anonymous' ? 'You’ll log in or create an account next. ' : ''}
          Nothing is charged until you pay.
        </p>
        {isFlexible && (
          <p className="text-center text-xs text-muted-foreground">
            Hotel rooms are confirmed within 24 hours - if they can’t be, you get a full refund.
          </p>
        )}
      </div>
    </div>
  )
}

/** Fixed packages: one button per date. Sold-out dates stay visible but can't be picked. */
function DeparturePicker({
  departures,
  isPending,
  isError,
  onRetry,
  selectedId,
  onSelect,
}: {
  departures: AvailableDeparture[] | undefined
  isPending: boolean
  isError: boolean
  onRetry: () => void
  selectedId: string | null
  onSelect: (id: string) => void
}) {
  if (isPending) {
    return (
      <div className="grid gap-2">
        {Array.from({ length: 3 }, (_, i) => (
          <Skeleton key={i} className="h-14 rounded-lg" />
        ))}
      </div>
    )
  }

  if (isError || !departures) {
    return (
      <div className="grid justify-items-start gap-2 text-sm">
        <p className="text-muted-foreground">Dates couldn’t be loaded.</p>
        <Button variant="outline" size="sm" onClick={onRetry}>
          Try again
        </Button>
      </div>
    )
  }

  if (departures.length === 0) {
    return (
      <div className="grid gap-2 rounded-lg bg-muted/50 p-3 text-sm">
        <p className="font-medium">No dates open right now</p>
        <p className="text-muted-foreground">New dates are added often. Call us and we’ll tell you when the next group leaves.</p>
        <a href={`tel:${site.phone.replace(/[^+\d]/g, '')}`} className="flex items-center gap-1.5 font-medium text-primary">
          <PhoneIcon className="size-4" />
          {site.phone}
        </a>
      </div>
    )
  }

  return (
    // Long lists scroll inside the box, so the price and the button stay close.
    <div role="group" aria-label="Departure dates" className="-mx-1 grid max-h-80 gap-2 overflow-y-auto px-1 py-1">
      {departures.map((d) => {
        const soldOut = d.seatsLeft === 0
        const selected = d.id === selectedId
        return (
          <button
            key={d.id}
            type="button"
            disabled={soldOut}
            aria-pressed={selected}
            onClick={() => onSelect(d.id)}
            className={cn(
              'flex items-center justify-between gap-3 rounded-lg border p-3 text-left text-sm transition-colors hover:bg-muted focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none disabled:cursor-not-allowed disabled:opacity-50 disabled:hover:bg-transparent',
              selected && 'border-primary bg-primary/5 ring-1 ring-primary hover:bg-primary/5',
            )}
          >
            <span className="grid">
              <span className="font-medium">{formatDate(d.startDate)}</span>
              <span className="text-xs text-muted-foreground">to {formatDate(d.endDate)}</span>
            </span>
            <span className="grid text-right">
              <span className="font-semibold tabular-nums">{formatTaka(d.adultPrice)}</span>
              <span
                className={cn(
                  'text-xs',
                  soldOut ? 'text-destructive' : d.seatsLeft <= 5 ? 'font-medium text-amber-600 dark:text-amber-500' : 'text-muted-foreground',
                )}
              >
                {soldOut ? 'Sold out' : `${d.seatsLeft} seat${d.seatsLeft === 1 ? '' : 's'} left`}
              </span>
            </span>
          </button>
        )
      })}
    </div>
  )
}

/** Flexible stays: a check-in date (the phone's own date picker) and how many nights. */
function FlexibleStayPicker({
  pkg,
  startDate,
  onStartDateChange,
  nights,
  onNightsChange,
  tooSoon,
}: {
  pkg: PackageDetails
  startDate: string
  onStartDateChange: (value: string) => void
  nights: number
  onNightsChange: (value: number) => void
  tooSoon: boolean
}) {
  const minNights = pkg.minNights ?? 1
  const maxNights = pkg.maxNights ?? minNights
  const nightOptions = Array.from({ length: maxNights - minNights + 1 }, (_, i) => minNights + i)

  return (
    <div className="grid gap-3">
      <div className="grid grid-cols-2 gap-3">
        <div className="grid gap-1.5">
          <Label htmlFor="check-in">Check-in</Label>
          <Input
            id="check-in"
            type="date"
            className="h-9"
            min={pkg.earliestStartDate ?? undefined}
            value={startDate}
            onChange={(e) => onStartDateChange(e.target.value)}
            aria-invalid={tooSoon}
          />
        </div>
        <div className="grid gap-1.5">
          <Label htmlFor="nights">Nights</Label>
          <Select value={String(nights)} onValueChange={(v) => onNightsChange(Number(v))}>
            <SelectTrigger id="nights" className="h-9 w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent position="popper">
              {nightOptions.map((n) => (
                <SelectItem key={n} value={String(n)}>
                  {n} night{n === 1 ? '' : 's'}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </div>
      </div>

      {tooSoon && pkg.earliestStartDate ? (
        <p className="text-xs text-destructive">The earliest check-in is {formatDate(pkg.earliestStartDate)}.</p>
      ) : startDate ? (
        <p className="text-xs text-muted-foreground">Check-out: {formatDate(addDays(startDate, nights))}</p>
      ) : null}
    </div>
  )
}

/** Adults / children / infants with − and + buttons. The limits match the API's traveller rules. */
function TravellerPicker({ value, onChange }: { value: Travellers; onChange: (value: Travellers) => void }) {
  const { adults, children, infants } = value
  const total = adults + children + infants

  return (
    <div className="grid gap-3">
      <Counter
        label="Adults"
        hint="12 years and over"
        value={adults}
        min={1}
        max={adults + (maxTravellers - total)}
        // Fewer adults than infants isn't allowed (each infant sits on an adult's lap), so infants follow.
        onChange={(n) => onChange({ ...value, adults: n, infants: Math.min(infants, n) })}
      />
      <Counter
        label="Children"
        hint="2–11 years"
        value={children}
        min={0}
        max={children + (maxTravellers - total)}
        onChange={(n) => onChange({ ...value, children: n })}
      />
      <Counter
        label="Infants"
        hint="Under 2 · on a lap, no seat"
        value={infants}
        min={0}
        max={Math.min(adults, infants + (maxTravellers - total))}
        onChange={(n) => onChange({ ...value, infants: n })}
      />
    </div>
  )
}

function Counter({
  label,
  hint,
  value,
  min,
  max,
  onChange,
}: {
  label: string
  hint: string
  value: number
  min: number
  max: number
  onChange: (value: number) => void
}) {
  return (
    <div className="flex items-center justify-between gap-3">
      <div className="grid">
        <span className="text-sm font-medium">{label}</span>
        <span className="text-xs text-muted-foreground">{hint}</span>
      </div>
      <div className="flex items-center gap-2">
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          disabled={value <= min}
          onClick={() => onChange(value - 1)}
          aria-label={`Fewer ${label.toLowerCase()}`}
        >
          <MinusIcon />
        </Button>
        <span className="w-6 text-center text-sm font-medium tabular-nums">{value}</span>
        <Button
          variant="outline"
          size="icon-sm"
          className="rounded-full"
          disabled={value >= max}
          onClick={() => onChange(value + 1)}
          aria-label={`More ${label.toLowerCase()}`}
        >
          <PlusIcon />
        </Button>
      </div>
    </div>
  )
}

/** The API's messages are written for customers ("Only 3 seats left..."), so they're shown as they are. */
function firstMessage(error: unknown): string {
  const appError = toAppError(error)
  return Object.values(appError.fieldErrors)[0] ?? appError.message
}
