import { CheckIcon } from 'lucide-react'
import { cn } from '@/lib/utils'

const checkoutSteps = ['Travellers', 'Payment', 'Confirmed'] as const

/**
 * Where the customer is in buying a trip: 1 Travellers → 2 Payment →
 * 3 Confirmed. Done steps get a tick, the current one is highlighted;
 * reaching "Confirmed" ticks them all. Shown on the checkout, payment and
 * payment-result pages - and when accepting a custom trip's quote, which is
 * that trip's step 1.
 */
export function CheckoutSteps({ current, className }: { current: 1 | 2 | 3; className?: string }) {
  const finished = current === checkoutSteps.length

  return (
    <nav aria-label="Booking steps" className={cn('w-full animate-fade-in', className)}>
      <ol className="flex items-start sm:items-center">
        {checkoutSteps.map((label, i) => {
          const step = i + 1
          const done = step < current || finished
          const active = step === current
          return (
            <li key={label} aria-current={active ? 'step' : undefined} className={cn('flex items-start sm:items-center', i > 0 && 'flex-1')}>
              {/* The line leading into this step: green once the customer has got this far. */}
              {i > 0 && (
                <span aria-hidden className={cn('mx-2 mt-4 h-0.5 min-w-4 flex-1 rounded-full sm:mx-3 sm:mt-0', step <= current ? 'bg-forest-500' : 'bg-ink-200')} />
              )}
              <span className="flex flex-col items-center gap-1.5 sm:flex-row sm:gap-2.5">
                <span
                  className={cn(
                    'flex size-8 shrink-0 items-center justify-center rounded-full text-sm font-bold transition-colors duration-300',
                    done && 'bg-forest-600 text-white shadow-[0_4px_12px_-4px_rgb(31_111_81/0.6)]',
                    done && active && 'ring-4 ring-forest-100',
                    !done && active && 'bg-primary text-white ring-4 ring-forest-100',
                    !done && !active && 'bg-card text-ink-400 ring-1 ring-ink-300 ring-inset',
                  )}
                >
                  {done ? <CheckIcon className="size-4" strokeWidth={3} aria-hidden /> : step}
                </span>
                <span className={cn('text-xs font-semibold whitespace-nowrap sm:text-sm', active ? 'text-ink-900' : done ? 'text-forest-700' : 'text-ink-400')}>
                  {label}
                  <span className="sr-only">{done ? ' (done)' : active ? ' (current step)' : ''}</span>
                </span>
              </span>
            </li>
          )
        })}
      </ol>
    </nav>
  )
}
