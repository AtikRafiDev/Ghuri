import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import { Slot } from "radix-ui"

/**
 * Small status pills. The tone variants (success / warning / danger / info /
 * neutral) carry a status's meaning; pair them with a word (and the dot) -
 * never colour alone.
 */
const badgeVariants = cva(
  "group/badge inline-flex h-6 w-fit shrink-0 items-center justify-center gap-1.5 overflow-hidden rounded-full border border-transparent px-2.5 text-xs font-semibold whitespace-nowrap transition-colors focus-visible:ring-4 focus-visible:ring-ring/20 has-data-[icon=inline-end]:pr-2 has-data-[icon=inline-start]:pl-2 [&>svg]:pointer-events-none [&>svg]:size-3.5!",
  {
    variants: {
      variant: {
        default: "bg-primary text-primary-foreground [a]:hover:bg-primary-hover",
        secondary: "bg-secondary text-secondary-foreground [a]:hover:bg-forest-200",
        destructive: "bg-clay-50 text-clay-700 ring-1 ring-clay-100 ring-inset",
        outline: "border-border bg-card text-ink-700 [a]:hover:bg-accent",
        ghost: "hover:bg-accent hover:text-accent-foreground",
        link: "text-forest-700 underline-offset-4 hover:underline",
        success: "bg-forest-50 text-forest-700 ring-1 ring-forest-100 ring-inset",
        warning: "bg-sun-50 text-sun-700 ring-1 ring-sun-100 ring-inset",
        danger: "bg-clay-50 text-clay-700 ring-1 ring-clay-100 ring-inset",
        info: "bg-forest-100 text-forest-800 ring-1 ring-forest-200 ring-inset",
        neutral: "bg-ink-100 text-ink-600 ring-1 ring-ink-200 ring-inset",
      },
    },
    defaultVariants: {
      variant: "default",
    },
  }
)

function Badge({
  className,
  variant = "default",
  asChild = false,
  dot = false,
  children,
  ...props
}: React.ComponentProps<"span"> &
  VariantProps<typeof badgeVariants> & { asChild?: boolean; dot?: boolean }) {
  const Comp = asChild ? Slot.Root : "span"

  return (
    <Comp
      data-slot="badge"
      data-variant={variant}
      className={cn(badgeVariants({ variant }), className)}
      {...props}
    >
      {/* asChild: the child IS the badge, so it must stay the only child (no dot then). */}
      {dot && !asChild ? (
        <>
          <span aria-hidden className="size-1.5 shrink-0 rounded-full bg-current" />
          {children}
        </>
      ) : (
        children
      )}
    </Comp>
  )
}

export { Badge, badgeVariants }
