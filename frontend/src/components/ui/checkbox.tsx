import * as React from "react"
import { cn } from "cn"
import { Checkbox as CheckboxPrimitive } from "radix-ui"
import { CheckIcon } from "lucide-react"

function Checkbox({
  className,
  ...props
}: React.ComponentProps<typeof CheckboxPrimitive.Root>) {
  return (
    <CheckboxPrimitive.Root
      data-slot="checkbox"
      className={cn(
        // after: a bigger invisible hit area around the 18px box.
        "peer relative flex size-[18px] shrink-0 items-center justify-center rounded-md border border-input bg-card shadow-soft transition-[background-color,border-color,box-shadow] duration-200 outline-none after:absolute after:-inset-x-3 after:-inset-y-2 hover:border-forest-400 focus-visible:border-forest-500 focus-visible:ring-4 focus-visible:ring-forest-500/15 disabled:cursor-not-allowed disabled:opacity-50 aria-invalid:border-clay-500 aria-invalid:ring-4 aria-invalid:ring-clay-500/12 data-checked:border-primary data-checked:bg-primary data-checked:text-primary-foreground",
        className
      )}
      {...props}
    >
      {/* The indicator only exists while checked - so the tick pops in every time it's ticked. */}
      <CheckboxPrimitive.Indicator data-slot="checkbox-indicator" className="grid animate-scale-in place-content-center text-current [&>svg]:size-3.5">
        <CheckIcon strokeWidth={3} />
      </CheckboxPrimitive.Indicator>
    </CheckboxPrimitive.Root>
  )
}

export { Checkbox }
