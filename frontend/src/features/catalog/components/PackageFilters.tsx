import { useId, useState, type FormEvent } from 'react'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectSeparator, SelectTrigger, SelectValue } from '@/components/ui/select'
import { cn } from '@/lib/utils'
import type { CategorySummary, DestinationSummary, PackageSearchParams } from '../api/catalog.api'
import { modeLabels, type PackageSearch } from '../lib/packageSearch'

const any = 'any' // Radix Select can't use "" as a value, so "no filter" is this.

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
 */
export function PackageFilters({ search, onChange, destinations, categories }: Props) {
  // The panel is on the page twice (sidebar + phone sheet): useId keeps each copy's label→field ids unique.
  const id = useId()
  const national = destinations.filter((d) => !d.isInternational)
  const international = destinations.filter((d) => d.isInternational)

  return (
    <div className="grid gap-5">
      <div className="grid gap-1.5">
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

      <div className="grid gap-1.5">
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

      <div className="grid gap-1.5">
        <span className="text-sm font-medium" id={`${id}-mode`}>
          Dates
        </span>
        <div role="group" aria-labelledby={`${id}-mode`} className="grid grid-cols-3 gap-1 rounded-lg bg-muted p-1">
          {([undefined, 'FixedDepartures', 'FlexibleStay'] as const).map((mode) => (
            <button
              key={mode ?? any}
              type="button"
              aria-pressed={search.mode === mode}
              onClick={() => onChange({ mode })}
              className={cn(
                'rounded-md px-2 py-1.5 text-xs font-medium text-muted-foreground transition-colors hover:text-foreground focus-visible:ring-2 focus-visible:ring-ring focus-visible:outline-none',
                search.mode === mode && 'bg-background text-foreground shadow-sm',
              )}
            >
              {mode ? modeLabels[mode] : 'All'}
            </button>
          ))}
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
    <form onSubmit={onSubmit} className="grid gap-1.5">
      <span className="text-sm font-medium">Price per person (৳)</span>
      <div className="flex items-center gap-2">
        <Input
          inputMode="numeric"
          placeholder="Min"
          aria-label="Lowest price"
          value={min}
          onChange={(e) => setMin(e.target.value.replace(/\D/g, ''))}
        />
        <span className="text-muted-foreground">–</span>
        <Input
          inputMode="numeric"
          placeholder="Max"
          aria-label="Highest price"
          value={max}
          onChange={(e) => setMax(e.target.value.replace(/\D/g, ''))}
        />
      </div>
      <Button type="submit" variant="outline" size="sm" className="justify-self-start">
        Apply price
      </Button>
    </form>
  )
}
