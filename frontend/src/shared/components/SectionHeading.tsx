import { ArrowRightIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import { Reveal } from '@/shared/motion/Reveal'

/**
 * Every section starts the same way: a small coloured eyebrow, the title,
 * and an optional line under it, rising in as they scroll into view.
 * The "See all" link sits on the title's row and shares its baseline
 * (items-baseline), so it lines up the same in every section.
 * tone="light": white text, for sections on a dark photo.
 */
export function SectionHeading({
  eyebrow,
  title,
  text,
  link,
  tone = 'dark',
  className,
}: {
  eyebrow: string
  title: string
  text?: string
  link?: { to: string; label: string }
  tone?: 'dark' | 'light'
  className?: string
}) {
  const light = tone === 'light'
  return (
    <Reveal y={24} className={cn('grid gap-3', className)}>
      <p className={cn('flex items-center gap-2 text-xs font-bold tracking-[0.18em] uppercase', light ? 'text-sun-300' : 'text-forest-600')}>
        <span aria-hidden className={cn('h-px w-8', light ? 'bg-sun-300' : 'bg-forest-500')} />
        {eyebrow}
      </p>
      <div className="flex flex-wrap items-baseline justify-between gap-x-6 gap-y-2">
        <h2 className={cn('max-w-2xl text-3xl leading-[1.1] font-extrabold sm:text-[2.75rem]', light ? 'text-white' : 'text-ink-900')}>{title}</h2>
        {link && (
          <Button asChild variant="link" className="text-[0.9375rem]">
            <Link to={link.to}>
              {link.label}
              <ArrowRightIcon />
            </Link>
          </Button>
        )}
      </div>
      {text && <p className={cn('max-w-xl sm:text-lg', light ? 'text-forest-100/80' : 'text-ink-500')}>{text}</p>}
    </Reveal>
  )
}
