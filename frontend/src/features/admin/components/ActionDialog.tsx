import { useState, type FormEvent, type ReactNode } from 'react'
import { Button } from '@/components/ui/button'
import { Dialog, DialogContent, DialogDescription, DialogFooter, DialogHeader, DialogTitle } from '@/components/ui/dialog'
import { Spinner } from '@/components/ui/spinner'
import { toAppError } from '@/shared/api/problem'
import { FormAlert } from '@/shared/components/FormAlert'

type ActionDialogProps = {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: ReactNode
  /** The form's inputs. */
  children: ReactNode
  submitLabel: string
  destructive?: boolean
  /** Does the work. If it throws, the API's message is shown and the dialog stays open. */
  onSubmit: () => Promise<void>
}

/**
 * A small form in a dialog for one admin action - "Record payment",
 * "Cancel booking", "Mark refunded"... Stays open, with the API's message,
 * if the action fails; can't be closed half-way.
 */
export function ActionDialog({ open, onOpenChange, title, description, children, submitLabel, destructive, onSubmit }: ActionDialogProps) {
  const [pending, setPending] = useState(false)
  const [error, setError] = useState<string | null>(null)

  const handleOpenChange = (next: boolean) => {
    if (pending) return
    if (!next) setError(null)
    onOpenChange(next)
  }

  const submit = async (event: FormEvent) => {
    event.preventDefault()
    setPending(true)
    setError(null)
    try {
      await onSubmit()
      onOpenChange(false)
    } catch (err) {
      setError(toAppError(err).message)
    } finally {
      setPending(false)
    }
  }

  return (
    <Dialog open={open} onOpenChange={handleOpenChange}>
      <DialogContent>
        <form onSubmit={submit} className="grid gap-4" noValidate>
          <DialogHeader>
            <DialogTitle>{title}</DialogTitle>
            <DialogDescription>{description}</DialogDescription>
          </DialogHeader>
          {children}
          {error && <FormAlert kind="error">{error}</FormAlert>}
          <DialogFooter>
            <Button type="button" variant="outline" disabled={pending} onClick={() => handleOpenChange(false)}>
              Close
            </Button>
            <Button type="submit" variant={destructive ? 'destructive' : 'default'} disabled={pending}>
              {pending && <Spinner />}
              {submitLabel}
            </Button>
          </DialogFooter>
        </form>
      </DialogContent>
    </Dialog>
  )
}
