import { useId, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import type { CategorySummary, DestinationSummary, PackageSearchParams } from '../api/catalog.api'
import { modeLabels, type PackageSearch } from '../lib/packageSearch'

const any = 'any' // Radix Select can't use "" as a value, so "no filter" is this.

// The caption above a group that isn't a single field ("Dates", "Price") - the exact look of <Label>, so every caption matches.
const captionClass = 'text-[0.8125rem] leading-none font-semibold text-ink-700'

const modeOptions = [
  { mode: undefined, label: 'All' },
  { mode: 'FixedDepartures', label: modeLabels.FixedDepartures },
  { mode: 'FlexibleStay', label: modeLabels.FlexibleStay },
] as const

type Props = {
  search: PackageSearch
  onChange: (changes: Partial<PackageSearchParams>) => void
  destinations: DestinationSummary[]
  categories: CategorySummary[]
}

/**
 * Destination, category, trip type and price. Each change goes straight
 * into the URL (onChange), so the results update right away - no "Apply".
 * Shown in the sidebar on a laptop and in a slide-out sheet on a phone.
 * Every group is caption + control with the same gap-2, so captions and
 * fields form one even column.
 */
export function PackageFilters({ search, onChange, destinations, categories }: Props) {
  // The panel is on the page twice (sidebar + phone sheet): useId keeps each copy's label→field ids unique.
  const id = useId()
  const national = destinations.filter((d) => !d.isInternational)
  const international = destinations.filter((d) => d.isInternational)

  return (
    <div className="grid gap-6">
      <div className="grid gap-2">
        <Label htmlFor={`${id}-destination`}>Destination</Label>
        <Select
          value={search.destination ?? any}
          onValueChange={(v) => onChange({ destination: v === any ? undefined : v })}
        >
          <SelectTrigger id={`${id}-destination`} className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper" className="max-h-72">
            <SelectItem value={any}>Anywhere</SelectItem>
            {national.length > 0 && <SelectSeparator />}
            {national.map((d) => (
              <SelectItem key={d.id} value={d.slug}>
                {d.name}
              </SelectItem>
            ))}
            {international.length > 0 && <SelectSeparator />}
            {international.map((d) => (
              <SelectItem key={d.id} value={d.slug}>
                {d.name} <span className="text-muted-foreground">· {d.countryName}</span>
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="grid gap-2">
        <Label htmlFor={`${id}-category`}>Type of trip</Label>
        <Select value={search.category ?? any} onValueChange={(v) => onChange({ category: v === any ? undefined : v })}>
          <SelectTrigger id={`${id}-category`} className="w-full">
            <SelectValue />
          </SelectTrigger>
          <SelectContent position="popper" className="max-h-72">
            <SelectItem value={any}>All types</SelectItem>
            {categories.map((c) => (
              <SelectItem key={c.id} value={c.slug}>
                {c.name}
              </SelectItem>
            ))}
          </SelectContent>
        </Select>
      </div>

      <div className="grid gap-2">
        <span className={captionClass} id={`${id}-mode`}>
          Dates
        </span>
        {/* A segmented control: one track, the chosen option lifts out of it as a white pill. flex-auto lets each
            option take its own text width plus an equal share of the rest, so no label has to wrap. */}
        <div role="group" aria-labelledby={`${id}-mode`} className="flex gap-1 rounded-xl bg-ink-100 p-1 ring-1 ring-ink-200/70 ring-inset">
          {modeOptions.map(({ mode, label }) => {
            const active = search.mode === mode
            return (
              <button
                key={mode ?? any}
                type="button"
                aria-pressed={active}
                onClick={() => onChange({ mode })}
                className={cn(
                  'flex h-9 flex-auto items-center justify-center rounded-lg px-2.5 text-xs font-semibold whitespace-nowrap text-ink-500 transition-[background-color,color,box-shadow] duration-200 hover:text-ink-900 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none',
                  active && 'bg-card text-forest-800 shadow-soft ring-1 ring-ink-200/80 hover:text-forest-800',
                )}
              >
                {label}
              </button>
            )
          })}
        </div>
      </div>

      {/* key: when the URL's prices change (e.g. "Clear filters"), the boxes start again from them. */}
      <PriceFilter
        key={`${search.minPrice ?? ''}-${search.maxPrice ?? ''}`}
        minPrice={search.minPrice}
        maxPrice={search.maxPrice}
        onApply={(minPrice, maxPrice) => onChange({ minPrice, maxPrice })}
      />
    </div>
  )
}

/**
 * Two number boxes and "Apply". Unlike the other filters this waits for
 * Apply (or Enter): searching on every keystroke would send "1", "15",
 * "150", "1500"... as separate searches.
 */
function PriceFilter({
  minPrice,
  maxPrice,
  onApply,
}: {
  minPrice: number | undefined
  maxPrice: number | undefined
  onApply: (minPrice: number | undefined, maxPrice: number | undefined) => void
}) {
  const [min, setMin] = useState(minPrice?.toString() ?? '')
  const [max, setMax] = useState(maxPrice?.toString() ?? '')

  const toPrice = (text: string) => (/^\d{1,9}$/.test(text.trim()) ? Number(text.trim()) : undefined)

  const onSubmit = (event: FormEvent) => {
    event.preventDefault()
    let from = toPrice(min)
    let to = toPrice(max)
    if (from !== undefined && to !== undefined && to < from) [from, to] = [to, from]
    onApply(from, to)
  }

  return (
    <form onSubmit={onSubmit} className="grid gap-2">
      <span className={captionClass}>Price per person</span>
      {/* min – max on one row; the Apply button below spans the same width, so all three edges line up. */}
      <div className="grid grid-cols-[minmax(0,1fr)_auto_minmax(0,1fr)] items-center gap-2">
        <TakaInput placeholder="Min" label="Lowest price" value={min} onChange={setMin} />
        <span aria-hidden className="text-ink-300">–</span>
        <TakaInput placeholder="Max" label="Highest price" value={max} onChange={setMax} />
      </div>
      <Button type="submit" variant="outline" className="w-full">
        Apply price
      </Button>
    </form>
  )
}

/** A number box with a "৳" in front of the digits. Only digits get in. */
function TakaInput({ placeholder, label, value, onChange }: { placeholder: string; label: string; value: string; onChange: (value: string) => void }) {
  return (
    <div className="relative">
      <span aria-hidden className="pointer-events-none absolute top-1/2 left-3.5 -translate-y-1/2 text-[0.9375rem] font-semibold text-ink-400">
        ৳
      </span>
      <Input
        inputMode="numeric"
        placeholder={placeholder}
        aria-label={label}
        value={value}
        onChange={(e) => onChange(e.target.value.replace(/\D/g, ''))}
        className="pl-8"
      />
    </div>
  )
}
