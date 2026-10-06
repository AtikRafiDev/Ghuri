import { useQuery } from '@tanstack/react-query'
import {
  ArrowRightIcon,
  CalendarCheckIcon,
  CalendarDaysIcon,
  CheckIcon,
  CircleAlertIcon,
  FlameIcon,
  MinusIcon,
  PhoneIcon,
  PlusIcon,
  ShieldCheckIcon,
} from 'lucide-react'
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
import { FormAlert } from '@/shared/components/FormAlert'
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
    // Kept compact on purpose: on a laptop the whole box (down to "Book now") should fit on screen while it stays in view.
    <div className="grid gap-4 rounded-3xl bg-card p-5 shadow-card ring-1 ring-ink-200/80">
      <h2 className="text-lg font-bold text-ink-900">{isFlexible ? 'Choose your stay' : 'Choose a date'}</h2>

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

      <div className="grid gap-4" aria-live="polite">
        {quoteError ? (
          <FormAlert kind="error">{quoteError}</FormAlert>
        ) : params === null ? (
          <p className="flex items-center gap-2 rounded-xl bg-ink-50 px-3.5 py-3 text-sm text-ink-500 ring-1 ring-ink-200/70 ring-inset">
            <CalendarDaysIcon className="size-4 shrink-0 text-ink-400" />
            {isFlexible ? 'Pick a check-in date to see the price.' : 'Pick a date to see the price.'}
          </p>
        ) : quote.data ? (
          <div className={cn('transition-opacity duration-300', quote.isFetching && 'opacity-60')}>
            <PriceBreakdown quote={quote.data} />
          </div>
        ) : (
          <div className="grid gap-3">
            <Skeleton className="h-9 w-full" />
            <Skeleton className="h-9 w-full" />
            <Skeleton className="h-7 w-full" />
          </div>
        )}

        <Button size="lg" className="w-full" disabled={!canBook} onClick={onBook}>
          {quote.isFetching && params !== null ? <Spinner /> : null}
          Book now
          <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
        </Button>

        <div className="grid gap-1.5 text-center text-xs text-ink-500">
          {/* The shield sits inline with the words, so it stays beside the text when the line wraps. */}
          <p className="text-balance">
            <ShieldCheckIcon className="mr-1 inline size-3.5 -translate-y-px text-forest-600" />
            {status === 'anonymous' ? 'You’ll log in or create an account next. ' : ''}
            Nothing is charged until you pay.
          </p>
          {isFlexible && <p>Hotel rooms are confirmed within 24 hours - if they can’t be, you get a full refund.</p>}
        </div>
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
          <Skeleton key={i} className="h-[3.625rem] rounded-2xl" />
        ))}
      </div>
    )
  }

  if (isError || !departures) {
    return (
      <div className="grid justify-items-start gap-3 rounded-2xl bg-ink-50 p-4 text-sm ring-1 ring-ink-200/70 ring-inset">
        <p className="text-ink-600">Dates couldn’t be loaded.</p>
        <Button variant="outline" size="sm" onClick={onRetry}>
          Try again
        </Button>
      </div>
    )
  }

  if (departures.length === 0) {
    return (
      <div className="grid gap-2 rounded-2xl bg-ink-50 p-4 text-sm ring-1 ring-ink-200/70 ring-inset">
        <p className="font-semibold text-ink-900">No dates open right now</p>
        <p className="text-ink-500">New dates are added often. Call us and we’ll tell you when the next group leaves.</p>
        <a
          href={`tel:${site.phone.replace(/[^+\d]/g, '')}`}
          className="mt-1 flex w-fit items-center gap-2 font-semibold text-forest-700 underline-offset-4 hover:underline"
        >
          <PhoneIcon className="size-4" />
          {site.phone}
        </a>
      </div>
    )
  }

  return (
    // Long lists scroll inside the box, so the price and the button stay close. -m-1 p-1: room for the selected ring.
    <div role="group" aria-label="Departure dates" className="-m-1 grid max-h-68 gap-2 overflow-y-auto p-1">
      {departures.map((d) => {
        const soldOut = d.seatsLeft === 0
        const selected = d.id === selectedId
        const fewLeft = !soldOut && d.seatsLeft <= 5
        return (
          <button
            key={d.id}
            type="button"
            disabled={soldOut}
            aria-pressed={selected}
            onClick={() => onSelect(d.id)}
            className={cn(
              'flex items-center gap-3 rounded-2xl px-3.5 py-2.5 text-left ring-1 transition-[background-color,box-shadow] duration-200 ring-inset focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none',
              soldOut
                ? 'cursor-not-allowed bg-ink-50 ring-ink-200/80'
                : selected
                  ? 'bg-forest-50 ring-2 ring-forest-600'
                  : 'bg-card ring-ink-200 hover:bg-forest-50/50 hover:ring-forest-300',
            )}
          >
            {/* The radio-style dot: empty, or a filled forest circle with a tick when this date is chosen. */}
            <span
              aria-hidden
              className={cn(
                'flex size-5 shrink-0 items-center justify-center rounded-full ring-1 transition-colors duration-200 ring-inset',
                selected ? 'bg-forest-600 text-white ring-forest-600' : soldOut ? 'bg-ink-100 ring-ink-200' : 'bg-card ring-ink-300',
              )}
            >
              {selected && <CheckIcon className="size-3 animate-scale-in" strokeWidth={3} />}
            </span>
            <span className="grid min-w-0 flex-1 gap-0.5">
              <span className={cn('text-sm font-semibold', soldOut ? 'text-ink-400' : 'text-ink-900')}>{formatDate(d.startDate)}</span>
              <span className={cn('text-xs', soldOut ? 'text-ink-400' : 'text-ink-500')}>to {formatDate(d.endDate)}</span>
            </span>
            <span className="grid shrink-0 justify-items-end gap-0.5">
              <span className={cn('nums text-sm font-bold', soldOut ? 'text-ink-400 line-through' : 'text-ink-900')}>
                {formatTaka(d.adultPrice)}
              </span>
              <span
                className={cn(
                  'flex items-center gap-1 text-xs',
                  soldOut ? 'font-semibold text-ink-400' : fewLeft ? 'font-semibold text-sun-700' : 'text-ink-500',
                )}
              >
                {fewLeft && <FlameIcon className="size-3.5" />}
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
        <div className="grid gap-2">
          <Label htmlFor="check-in">Check-in</Label>
          <Input
            id="check-in"
            type="date"
            min={pkg.earliestStartDate ?? undefined}
            value={startDate}
            onChange={(e) => onStartDateChange(e.target.value)}
            aria-invalid={tooSoon}
          />
        </div>
        <div className="grid gap-2">
          <Label htmlFor="nights">Nights</Label>
          <Select value={String(nights)} onValueChange={(v) => onNightsChange(Number(v))}>
            <SelectTrigger id="nights" className="w-full">
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
        <p className="flex animate-fade-in items-center gap-1.5 text-xs font-medium text-destructive">
          <CircleAlertIcon className="size-3.5 shrink-0" />
          The earliest check-in is {formatDate(pkg.earliestStartDate)}.
        </p>
      ) : startDate ? (
        <p className="flex items-center gap-1.5 text-xs text-ink-500">
          <CalendarCheckIcon className="size-3.5 shrink-0 text-forest-600" />
          Check-out: <span className="font-semibold text-ink-700">{formatDate(addDays(startDate, nights))}</span>
        </p>
      ) : null}
    </div>
  )
}

/**
 * Adults / children / infants with − and + buttons. The limits match the API's traveller rules.
 * Every stepper is the same width, so the three line up in one column on the right.
 */
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
      <div className="grid min-w-0 gap-0.5">
        <span className="text-sm font-semibold text-ink-900">{label}</span>
        <span className="text-xs text-ink-500">{hint}</span>
      </div>
      {/* − value + in one pill-shaped track; the value has a fixed width so every row's buttons line up. */}
      <div className="flex shrink-0 items-center rounded-full bg-ink-50 p-1 ring-1 ring-ink-200/80 ring-inset">
        <Button
          variant="outline"
          size="icon-sm"
          className="size-8 rounded-full"
          disabled={value <= min}
          onClick={() => onChange(value - 1)}
          aria-label={`Fewer ${label.toLowerCase()}`}
        >
          <MinusIcon />
        </Button>
        <span className="nums w-9 text-center text-sm font-bold text-ink-900">{value}</span>
        <Button
          variant="outline"
          size="icon-sm"
          className="size-8 rounded-full"
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
