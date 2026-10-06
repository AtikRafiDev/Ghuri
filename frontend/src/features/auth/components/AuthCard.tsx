import { LockIcon, SendIcon, ShieldCheckIcon, TimerIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { BrandMark } from '@/shared/components/BrandMark'

const promises = [
  { icon: LockIcon, text: 'Secure online payment by SSLCommerz' },
  { icon: TimerIcon, text: 'Your seats are held while you pay' },
  { icon: ShieldCheckIcon, text: 'Full refund 30+ days before the trip' },
]

/**
 * The frame every auth page sits in - one look for login, register, forgot
 * and reset. On a wide screen a green brand panel sits beside the form
 * (a paper plane flies its dashed route); on a phone it's just the form.
 */
export function AuthCard({
  title,
  description,
  footer,
  children,
}: {
  title: string
  description?: string
  footer?: ReactNode
  children: ReactNode
}) {
  return (
    <div className="mx-auto grid w-full max-w-5xl overflow-hidden rounded-[2rem] bg-card shadow-lift ring-1 ring-ink-200/80 lg:grid-cols-[1fr_1.05fr]">
      <BrandPanel />
      <div className="grid content-center gap-7 px-6 py-10 sm:px-12 sm:py-14">
        <header className="grid animate-fade-up gap-2">
          <h1 className="text-2xl font-bold text-ink-900 sm:text-3xl">{title}</h1>
          {description && <p className="text-ink-500">{description}</p>}
        </header>
        <div className="animate-[fade-up_0.6s_var(--ease-out-expo)_0.08s_backwards]">{children}</div>
        {footer && <footer className="animate-[fade-up_0.6s_var(--ease-out-expo)_0.16s_backwards] border-t pt-6 text-center text-sm text-ink-500">{footer}</footer>}
      </div>
    </div>
  )
}

function BrandPanel() {
  return (
    <aside className="brand-surface relative isolate hidden overflow-hidden bg-gradient-to-br from-forest-700 via-forest-800 to-forest-950 p-10 text-white lg:grid lg:content-between">
      <div aria-hidden className="bg-topo absolute inset-0 -z-10" />
      <div aria-hidden className="absolute -top-20 -right-16 -z-10 size-64 rounded-full bg-sun-500/20 blur-3xl" />
      <div aria-hidden className="absolute -bottom-24 -left-10 -z-10 size-72 rounded-full bg-forest-400/20 blur-3xl" />

      <div className="flex items-center gap-2.5 text-xl font-bold tracking-tight">
        <BrandMark className="size-9" />
        Ghuri
      </div>

      <FlightPath />

      <div className="grid gap-6">
        <div className="grid gap-2">
          <h2 className="text-3xl leading-tight font-bold">
            Your next trip is
            <br />
            <span className="text-sun-300">one step away.</span>
          </h2>
          <p className="max-w-sm text-forest-100/75">Hand-picked hotels, local guides and every transfer arranged - you just pack the bag.</p>
        </div>
        <ul className="stagger grid gap-3 text-sm">
          {promises.map(({ icon: Icon, text }) => (
            <li key={text} className="flex items-center gap-3">
              <span className="flex size-8 items-center justify-center rounded-lg bg-white/10 ring-1 ring-white/15">
                <Icon className="size-4 text-sun-300" />
              </span>
              {text}
            </li>
          ))}
        </ul>
      </div>
    </aside>
  )
}

// In CSS pixels: offset-path (the plane's track) uses the same coordinates as the drawn route, so the box keeps a fixed size.
const route = 'M 16 140 C 80 36, 150 184, 226 82 S 330 26, 364 62'

/** A dashed flight route between two pins, with a paper plane gliding along it (CSS offset-path). */
function FlightPath() {
  return (
    <div aria-hidden className="relative my-6 h-[180px] w-[380px] max-w-full">
      <svg width="380" height="180" className="absolute inset-0 overflow-visible">
        <path d={route} fill="none" stroke="white" strokeOpacity="0.35" strokeWidth="2" strokeDasharray="6 8" strokeLinecap="round" />
        <circle cx="16" cy="140" r="13" className="origin-center animate-[ring-ping_2s_ease-out_infinite] fill-sun-500/40 [transform-box:fill-box]" />
        <circle cx="16" cy="140" r="7" className="fill-sun-500" />
        <circle cx="364" cy="62" r="7" className="fill-white" />
      </svg>
      <div className="absolute top-0 left-0" style={{ offsetPath: `path('${route}')`, offsetRotate: 'auto', animation: 'fly 6s ease-in-out infinite' }}>
        <SendIcon className="size-6 rotate-45 fill-white/20 text-white drop-shadow-lg" />
      </div>
    </div>
  )
}
