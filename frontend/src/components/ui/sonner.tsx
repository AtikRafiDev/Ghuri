import type { CSSProperties } from "react"
import { Toaster as Sonner, type ToasterProps } from "sonner"

/**
 * Where toasts appear (mounted once, in app/providers.tsx). Each toast's
 * look is our own PremiumToast (shared/lib/notify.tsx) - sonner only
 * handles stacking, swipe-to-dismiss, timers and screen-reader announcements.
 */
function Toaster(props: ToasterProps) {
  return (
    <Sonner
      position="top-right"
      gap={12}
      offset={20}
      mobileOffset={12}
      visibleToasts={4}
      toastOptions={{ unstyled: true }}
      className="toaster group [&_[data-sonner-toast]]:w-full"
      style={{ "--width": "384px" } as CSSProperties}
      {...props}
    />
  )
}

export { Toaster }
