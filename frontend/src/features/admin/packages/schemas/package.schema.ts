import { z } from 'zod'
import { slugProblem } from '@/shared/lib/slug'
import { PricingMode, type AdminPackage, type PackageRequest, type TourType } from '../api/packages.api'

// The SAME limits as the API's PackageFieldsValidator (and PackagePricing) -
// instant feedback here, the API stays the real judge.
const maxDays = 60
const maxListItems = 30

/**
 * One flat object for both pricing modes, not a discriminated union: when
 * the admin flips Fixed ⇄ Flexible, the numbers typed for the other mode
 * stay in the form (flip back and they're still there). Which fields are
 * required is checked in superRefine, by mode.
 *
 * Number boxes are kept as TEXT: an emptied box is "", which is easy to
 * tell apart from 0. toRequest() turns them into numbers.
 */
export const packageSchema = z
  .object({
    title: z.string().trim().min(1, 'Enter a title.').max(200, 'At most 200 characters.'),
    slug: z.string().trim(),
    destinationId: z.string().min(1, 'Choose a destination.'),
    summary: z.string().trim().min(1, 'Enter a short summary.').max(1000, 'At most 1000 characters.'),
    description: z.string().trim().max(20_000, 'At most 20000 characters.'),
    tourType: z.enum(['1', '2', '3']),
    categoryIds: z.array(z.string()).max(10, 'At most 10 categories.'),
    // One point per line in a textarea.
    inclusions: z.string(),
    exclusions: z.string(),
    termsAndPolicy: z.string().trim().max(20_000, 'At most 20000 characters.'),
    minAge: z.string().trim(),
    isFeatured: z.boolean(),

    pricingMode: z.enum(['fixed', 'flexible']),
    durationDays: z.string().trim(),
    durationNights: z.string().trim(),
    minNights: z.string().trim(),
    maxNights: z.string().trim(),
    basePrice: z.string().trim(),
    extraNightPrice: z.string().trim(),
    minLeadDays: z.string().trim(),
  })
  .superRefine((v, ctx) => {
    const problem = slugProblem(v.slug, v.title, 220)
    if (problem) ctx.addIssue({ code: 'custom', path: ['slug'], message: problem })

    for (const field of ['inclusions', 'exclusions'] as const) {
      const lines = toLines(v[field])
      if (lines.length > maxListItems) ctx.addIssue({ code: 'custom', path: [field], message: `At most ${maxListItems} points.` })
      else if (lines.some((line) => line.length > 200)) ctx.addIssue({ code: 'custom', path: [field], message: 'Each point: at most 200 characters.' })
    }

    if (v.minAge !== '') checkWhole(ctx, 'minAge', v.minAge, 0, 100)

    if (v.pricingMode === 'fixed') {
      checkWhole(ctx, 'durationDays', v.durationDays, 1, maxDays, 'Enter the number of days.')
      checkWhole(ctx, 'durationNights', v.durationNights, 0, maxDays, 'Enter the number of nights.')
      return
    }

    // The longest stay (maxNights + 1 days) must fit the 60-day limit.
    const minOk = checkWhole(ctx, 'minNights', v.minNights, 1, maxDays - 1, 'Enter the minimum nights.')
    const maxOk = checkWhole(ctx, 'maxNights', v.maxNights, 1, maxDays - 1, 'Enter the maximum nights.')
    if (minOk && maxOk && Number(v.maxNights) < Number(v.minNights)) {
      ctx.addIssue({ code: 'custom', path: ['maxNights'], message: 'Cannot be less than the minimum nights.' })
    }
    checkMoney(ctx, 'basePrice', v.basePrice, { allowZero: false, missing: 'Enter the base price.' })
    checkMoney(ctx, 'extraNightPrice', v.extraNightPrice, { allowZero: true, missing: 'Enter the extra-night price (0 if free).' })
    checkWhole(ctx, 'minLeadDays', v.minLeadDays, 0, 90, 'Enter how many days ahead (0 = same day).')
  })

export type PackageInput = z.infer<typeof packageSchema>

type Ctx = z.RefinementCtx

/** A whole number between min and max. Returns true when fine. */
function checkWhole(ctx: Ctx, path: string, text: string, min: number, max: number, missing = 'Enter a number.'): boolean {
  if (text === '') {
    ctx.addIssue({ code: 'custom', path: [path], message: missing })
    return false
  }
  const value = Number(text)
  if (!Number.isInteger(value) || value < min || value > max) {
    ctx.addIssue({ code: 'custom', path: [path], message: `Enter a whole number from ${min} to ${max}.` })
    return false
  }
  return true
}

/** Taka with at most 2 decimals (the database column is decimal(18,2)). */
function checkMoney(ctx: Ctx, path: string, text: string, { allowZero, missing }: { allowZero: boolean; missing: string }) {
  if (text === '') return ctx.addIssue({ code: 'custom', path: [path], message: missing })
  const value = Number(text)
  if (Number.isNaN(value) || value < 0 || (!allowZero && value === 0)) {
    return ctx.addIssue({ code: 'custom', path: [path], message: allowZero ? 'Enter 0 or more.' : 'Enter more than 0.' })
  }
  if (!/^\d+(\.\d{1,2})?$/.test(text)) ctx.addIssue({ code: 'custom', path: [path], message: 'At most 2 decimals, e.g. 8000 or 7999.50.' })
}

/** Textarea → bullet points: one per line, blank lines dropped. */
function toLines(text: string): string[] {
  return text
    .split('\n')
    .map((line) => line.trim())
    .filter(Boolean)
}

const numberOrNull = (text: string) => (text === '' ? null : Number(text))

/** An existing package (or nothing, for a new one) → the form's starting values. */
export function toFormValues(p?: AdminPackage): PackageInput {
  const text = (value: number | null | undefined) => (value === null || value === undefined ? '' : String(value))
  const isFlexible = p?.pricingMode === PricingMode.FlexibleStay
  return {
    title: p?.title ?? '',
    slug: p?.slug ?? '',
    destinationId: p?.destinationId ?? '',
    summary: p?.summary ?? '',
    description: p?.description ?? '',
    tourType: String(p?.tourType ?? 1) as PackageInput['tourType'],
    categoryIds: p?.categoryIds ?? [],
    inclusions: p?.inclusions.join('\n') ?? '',
    exclusions: p?.exclusions.join('\n') ?? '',
    termsAndPolicy: p?.termsAndPolicy ?? '',
    minAge: text(p?.minAge),
    isFeatured: p?.isFeatured ?? false,
    pricingMode: isFlexible ? 'flexible' : 'fixed',
    // A flexible package's duration is worked out by the API - don't show it as typed values.
    durationDays: isFlexible ? '' : text(p?.durationDays),
    durationNights: isFlexible ? '' : text(p?.durationNights),
    minNights: text(p?.minNights),
    maxNights: text(p?.maxNights),
    basePrice: text(p?.basePrice),
    extraNightPrice: text(p?.extraNightPrice),
    minLeadDays: text(p?.minLeadDays ?? (p ? null : 2)),
  }
}

/**
 * Form values → API body. Empty text becomes null ("not set"); only the
 * chosen mode's numbers are sent, the other mode's are sent as null.
 */
export function toRequest(v: PackageInput): PackageRequest {
  const fixed = v.pricingMode === 'fixed'
  return {
    destinationId: v.destinationId,
    title: v.title,
    slug: v.slug || null,
    summary: v.summary,
    description: v.description || null,
    tourType: Number(v.tourType) as TourType,
    categoryIds: v.categoryIds,
    inclusions: toLines(v.inclusions),
    exclusions: toLines(v.exclusions),
    termsAndPolicy: v.termsAndPolicy || null,
    minAge: numberOrNull(v.minAge),
    isFeatured: v.isFeatured,
    pricingMode: fixed ? PricingMode.FixedDepartures : PricingMode.FlexibleStay,
    durationDays: fixed ? numberOrNull(v.durationDays) : null,
    durationNights: fixed ? numberOrNull(v.durationNights) : null,
    minNights: fixed ? null : numberOrNull(v.minNights),
    maxNights: fixed ? null : numberOrNull(v.maxNights),
    basePrice: fixed ? null : numberOrNull(v.basePrice),
    extraNightPrice: fixed ? null : numberOrNull(v.extraNightPrice),
    minLeadDays: fixed ? null : numberOrNull(v.minLeadDays),
  }
}
