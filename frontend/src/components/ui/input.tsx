import * as React from "react"
import { cn } from "cn"

/**
 * The look every text-like field shares (Input, Textarea, SelectTrigger):
 * white surface, soft shadow, green border on hover, green glow on focus,
 * terracotta ring when invalid. One string, so they can never disagree.
 */
const fieldClasses =
  "w-full min-w-0 rounded-xl border border-input bg-card text-base text-foreground shadow-soft transition-[border-color,box-shadow,background-color] duration-200 ease-out outline-none placeholder:text-ink-400 hover:border-forest-300 focus-visible:border-forest-500 focus-visible:ring-4 focus-visible:ring-forest-500/15 disabled:cursor-not-allowed disabled:bg-muted disabled:opacity-60 disabled:shadow-none aria-invalid:border-clay-500 aria-invalid:ring-4 aria-invalid:ring-clay-500/12 aria-invalid:hover:border-clay-500 md:text-sm"

function Input({ className, type, ...props }: React.ComponentProps<"input">) {
  return (
    <input
      type={type}
      data-slot="input"
      className={cn(
        fieldClasses,
        "h-10 px-3.5 py-2 file:inline-flex file:h-6 file:border-0 file:bg-transparent file:text-sm file:font-medium file:text-foreground [&::-webkit-calendar-picker-indicator]:cursor-pointer [&::-webkit-calendar-picker-indicator]:rounded-md [&::-webkit-calendar-picker-indicator]:opacity-50 [&::-webkit-calendar-picker-indicator]:transition-opacity hover:[&::-webkit-calendar-picker-indicator]:opacity-100",
        className
      )}
      {...props}
    />
  )
}

export { Input, fieldClasses }
