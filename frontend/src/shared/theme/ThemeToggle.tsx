import { MoonIcon, SunIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { setTheme, useTheme } from './theme'

/** Sun / moon button: the sun sets and the moon rises (and back). */
export function ThemeToggle({ className }: { className?: string }) {
  const theme = useTheme()
  const next = theme === 'dark' ? 'light' : 'dark'
  const label = theme === 'dark' ? 'Switch to light mode' : 'Switch to dark mode'

  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button variant="outline" size="icon" aria-label={label} onClick={() => setTheme(next)} className={cn('relative overflow-hidden', className)}>
          <SunIcon className="absolute transition-[rotate,scale,opacity] duration-500 ease-(--ease-spring) dark:-rotate-90 dark:scale-0 dark:opacity-0" />
          <MoonIcon className="absolute scale-0 rotate-90 opacity-0 transition-[rotate,scale,opacity] duration-500 ease-(--ease-spring) dark:scale-100 dark:rotate-0 dark:opacity-100" />
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}
