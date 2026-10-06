import { XIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { toast } from 'sonner'
import { Spinner } from '@/components/ui/spinner'
import { cn } from '@/lib/utils'

export type ToastTone = 'success' | 'error' | 'warning' | 'info' | 'loading'

export type PremiumToastProps = {
  id: string | number
  tone: ToastTone
  title: ReactNode
  description?: ReactNode
  action?: { label: string; onClick: () => void }
  /** ms - drives the time-left bar; Infinity (loading) hides it. */
  duration: number
}

const tones: Record<ToastTone, { icon: string; ring: string; bar: string }> = {
  success: { icon: 'bg-forest-600 text-white', ring: 'border-forest-300', bar: 'bg-forest-500' },
  error: { icon: 'bg-clay-500 text-white', ring: 'border-clay-300', bar: 'bg-clay-500' },
  warning: { icon: 'bg-sun-500 text-forest-950', ring: 'border-sun-300', bar: 'bg-sun-500' },
  info: { icon: 'bg-forest-100 text-forest-800', ring: 'border-forest-200', bar: 'bg-forest-400' },
  loading: { icon: 'bg-forest-50 text-forest-700', ring: 'border-transparent', bar: 'bg-transparent' },
}

/**
 * One toast card: a coloured badge whose symbol draws itself, the message,
 * an optional action, a close button and a thin bar showing the time left
 * (it pauses while the pointer is over the toasts - see sonner.tsx).
 */
export function PremiumToast({ id, tone, title, description, action, duration }: PremiumToastProps) {
  const style = tones[tone]
  return (
    <div
      className="group/toast relative flex w-full items-start gap-3 overflow-hidden rounded-2xl bg-card p-4 pr-11 text-sm shadow-pop ring-1 ring-ink-200 sm:w-(--width)"
      role={tone === 'error' ? 'alert' : 'status'}
    >
      <span className="relative mt-px flex size-9 shrink-0 items-center justify-center">
        {tone !== 'loading' && (
          <span aria-hidden className={cn('absolute inset-0 animate-[ring-ping_1.4s_ease-out_0.15s_both] rounded-full border-2', style.ring)} />
        )}
        <span className={cn('relative flex size-9 animate-scale-in items-center justify-center rounded-full', style.icon)}>
          <ToneSymbol tone={tone} />
        </span>
      </span>

      <div className="grid min-w-0 flex-1 gap-0.5 pt-0.5">
        <p className="leading-snug font-semibold text-ink-900">{title}</p>
        {description && <div className="text-[0.8125rem] leading-relaxed text-ink-500">{description}</div>}
        {action && (
          <button
            type="button"
            onClick={() => {
              action.onClick()
              toast.dismiss(id)
            }}
            className="mt-1.5 w-fit rounded-lg bg-forest-50 px-2.5 py-1 text-xs font-semibold text-forest-800 transition-colors hover:bg-forest-100"
          >
            {action.label}
          </button>
        )}
      </div>

      <button
        type="button"
        aria-label="Dismiss"
        onClick={() => toast.dismiss(id)}
        className="absolute top-3 right-3 flex size-7 items-center justify-center rounded-full text-ink-400 transition-[background-color,color,rotate] duration-300 hover:rotate-90 hover:bg-ink-100 hover:text-ink-900"
      >
        <XIcon className="size-4" />
      </button>

      {Number.isFinite(duration) && (
        <span
          aria-hidden
          className={cn('toast-progress absolute inset-x-0 bottom-0 h-[3px] origin-left', style.bar)}
          style={{ animation: `toast-progress ${duration}ms linear forwards` }}
        />
      )}
    </div>
  )
}

/** The symbol inside the badge, drawn stroke by stroke. */
function ToneSymbol({ tone }: { tone: ToastTone }) {
  if (tone === 'loading') return <Spinner className="size-[18px]" />
  const draw = 'animate-[draw_0.45s_var(--ease-out-expo)_0.2s_forwards]'
  return (
    <svg viewBox="0 0 24 24" className="size-[18px]" fill="none" stroke="currentColor" strokeWidth={3} strokeLinecap="round" strokeLinejoin="round" aria-hidden>
      {tone === 'success' && <path d="M5 12.5l4.5 4.5L19 7.5" strokeDasharray={24} strokeDashoffset={24} className={draw} />}
      {tone === 'error' && <path d="M7 7l10 10M17 7L7 17" strokeDasharray={30} strokeDashoffset={30} className={draw} />}
      {tone === 'warning' && (
        <>
          <path d="M12 6v7" strokeDasharray={8} strokeDashoffset={8} className={draw} />
          <circle cx="12" cy="18" r="0.6" fill="currentColor" />
        </>
      )}
      {tone === 'info' && (
        <>
          <circle cx="12" cy="6.5" r="0.6" fill="currentColor" />
          <path d="M12 11v7" strokeDasharray={8} strokeDashoffset={8} className={draw} />
        </>
      )}
    </svg>
  )
}
