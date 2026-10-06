import type { ReactNode } from 'react'
import { toast } from 'sonner'
import { PremiumToast, type ToastTone } from '@/shared/components/PremiumToast'
import { toAppError } from '@/shared/api/problem'

type Options = {
  description?: ReactNode
  action?: { label: string; onClick: () => void }
  /** ms the toast stays up. Defaults: 4.5s, errors 7s (more to read). */
  duration?: number
  /** Re-use an id to update a toast in place (e.g. loading → done). */
  id?: string | number
}

const defaultDuration: Record<ToastTone, number> = { success: 4500, info: 4500, warning: 6000, error: 7000, loading: Infinity }

function show(tone: ToastTone, title: ReactNode, options: Options = {}) {
  const duration = options.duration ?? defaultDuration[tone]
  return toast.custom(
    (id) => <PremiumToast id={id} tone={tone} title={title} description={options.description} action={options.action} duration={duration} />,
    { id: options.id, duration },
  )
}

/**
 * Pop-up messages for "something just happened" (saved, sent, deleted,
 * failed). They slide in at the corner, stack, and leave by themselves.
 *
 * Use them for the RESULT of an action. Keep messages that must stay
 * visible next to the thing they're about (a form's validation error, "this
 * booking is cancelled") in the page - with FormAlert - instead.
 *
 *   notify.success('Changes saved')
 *   notify.error(error)                       // any thrown error → its readable message
 *   notify.promise(save(), { loading: 'Saving…', success: 'Saved' })
 */
export const notify = {
  success: (title: ReactNode, options?: Options) => show('success', title, options),
  info: (title: ReactNode, options?: Options) => show('info', title, options),
  warning: (title: ReactNode, options?: Options) => show('warning', title, options),
  /** Pass a string, or the caught error itself - it's turned into the API's readable message. */
  error: (titleOrError: ReactNode | unknown, options?: Options) =>
    show('error', isNode(titleOrError) ? titleOrError : toAppError(titleOrError).message, options),
  loading: (title: ReactNode, options?: Options) => show('loading', title, options),
  dismiss: (id?: string | number) => toast.dismiss(id),

  /** One toast that follows a promise: a spinner while it runs, then success or the error. */
  async promise<T>(
    promise: Promise<T>,
    messages: { loading: ReactNode; success: ReactNode | ((value: T) => ReactNode); error?: ReactNode | ((error: unknown) => ReactNode) },
  ): Promise<T> {
    const id = show('loading', messages.loading)
    try {
      const value = await promise
      show('success', typeof messages.success === 'function' ? messages.success(value) : messages.success, { id })
      return value
    } catch (error) {
      const message = typeof messages.error === 'function' ? messages.error(error) : (messages.error ?? toAppError(error).message)
      show('error', message, { id })
      throw error
    }
  },
}

function isNode(value: unknown): value is ReactNode {
  return typeof value === 'string' || typeof value === 'number' || (typeof value === 'object' && value !== null && '$$typeof' in value)
}
