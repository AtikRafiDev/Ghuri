import { cn } from "cn"

/** A placeholder block with a soft sweeping shimmer while content loads. */
function Skeleton({ className, ...props }: React.ComponentProps<"div">) {
  return (
    <div
      data-slot="skeleton"
      className={cn("skeleton-shimmer rounded-lg", className)}
      {...props}
    />
  )
}

export { Skeleton }
