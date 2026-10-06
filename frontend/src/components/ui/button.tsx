import * as React from "react"
import { cva, type VariantProps } from "class-variance-authority"
import { cn } from "cn"
import { Slot } from "radix-ui"

// Heights are shared with Input / SelectTrigger (h-10 by default) so a
// button sitting next to a field always lines up edge to edge.
const buttonVariants = cva(
  "group/button relative inline-flex shrink-0 items-center justify-center rounded-xl border border-transparent bg-clip-padding text-sm font-semibold whitespace-nowrap transition-[background-color,border-color,color,box-shadow,translate,scale] duration-200 ease-out outline-none select-none focus-visible:ring-4 focus-visible:ring-ring/25 active:not-aria-[haspopup]:scale-[0.97] disabled:pointer-events-none disabled:opacity-50 aria-invalid:border-destructive aria-invalid:ring-4 aria-invalid:ring-destructive/15 [&_svg]:pointer-events-none [&_svg]:shrink-0 [&_svg:not([class*='size-'])]:size-4 [&_svg]:transition-[translate] [&_svg]:duration-300",
  {
    variants: {
      variant: {
        default:
          "bg-primary text-primary-foreground shadow-[inset_0_1px_0_rgb(255_255_255/0.12),0_1px_2px_rgb(15_59_44/0.2)] hover:bg-primary-hover hover:shadow-[inset_0_1px_0_rgb(255_255_255/0.12),0_8px_20px_-8px_rgb(15_59_44/0.55)] hover:-translate-y-px",
        accent:
          "bg-sun-500 text-forest-950 shadow-[inset_0_1px_0_rgb(255_255_255/0.3),0_1px_2px_rgb(143_92_16/0.25)] hover:bg-sun-300 hover:-translate-y-px hover:shadow-[0_8px_20px_-8px_rgb(143_92_16/0.5)]",
        outline:
          "border-border bg-card text-foreground shadow-soft hover:border-forest-300 hover:bg-accent hover:text-accent-foreground aria-expanded:border-forest-300 aria-expanded:bg-accent",
        secondary:
          "bg-secondary text-secondary-foreground hover:bg-forest-200 aria-expanded:bg-forest-200",
        ghost:
          "text-ink-600 hover:bg-accent hover:text-accent-foreground aria-expanded:bg-accent aria-expanded:text-accent-foreground",
        destructive:
          "bg-clay-50 text-destructive ring-1 ring-clay-100 ring-inset hover:bg-clay-500 hover:text-white hover:ring-clay-500 focus-visible:ring-destructive/20",
        link: "h-auto! px-0! text-forest-700 underline-offset-4 hover:underline [&_svg]:group-hover/button:translate-x-0.5",
      },
      size: {
        default:
          "h-10 gap-2 px-4 has-data-[icon=inline-end]:pr-3 has-data-[icon=inline-start]:pl-3",
        xs: "h-7 gap-1 rounded-lg px-2.5 text-xs [&_svg:not([class*='size-'])]:size-3.5",
        sm: "h-9 gap-1.5 rounded-lg px-3 text-[0.8125rem] [&_svg:not([class*='size-'])]:size-4",
        lg: "h-12 gap-2 rounded-2xl px-6 text-base [&_svg:not([class*='size-'])]:size-5",
        icon: "size-10",
        "icon-xs": "size-7 rounded-lg [&_svg:not([class*='size-'])]:size-3.5",
        "icon-sm": "size-9 rounded-lg",
        "icon-lg": "size-12 rounded-2xl",
      },
    },
    defaultVariants: {
      variant: "default",
      size: "default",
    },
  }
)

function Button({
  className,
  variant = "default",
  size = "default",
  asChild = false,
  ...props
}: React.ComponentProps<"button"> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }) {
  const Comp = asChild ? Slot.Root : "button"

  return (
    <Comp
      data-slot="button"
      data-variant={variant}
      data-size={size}
      className={cn(buttonVariants({ variant, size, className }))}
      {...props}
    />
  )
}

export { Button, buttonVariants }
