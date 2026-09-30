import { PencilIcon, Trash2Icon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'

type RowActionsProps = {
  /** What the row is, for screen readers: "Edit Cox's Bazar", "Delete Cox's Bazar". */
  name: string
  onEdit: () => void
  onDelete: () => void
  /** Set when the row can't be deleted - shown as a tooltip on the disabled delete button. */
  deleteBlockedReason?: string
}

/** The Edit / Delete buttons at the end of an admin table row. */
export function RowActions({ name, onEdit, onDelete, deleteBlockedReason }: RowActionsProps) {
  return (
    <div className="flex justify-end gap-1">
      <Button variant="ghost" size="icon-sm" aria-label={`Edit ${name}`} onClick={onEdit}>
        <PencilIcon />
      </Button>
      {deleteBlockedReason ? (
        <Tooltip>
          <TooltipTrigger asChild>
            {/* A disabled button gets no mouse or focus events, so the tooltip
                hangs on a focusable wrapper - keyboard users can reach it too. */}
            <span tabIndex={0}>
              <Button variant="ghost" size="icon-sm" disabled aria-label={`Delete ${name}`}>
                <Trash2Icon />
              </Button>
            </span>
          </TooltipTrigger>
          <TooltipContent>{deleteBlockedReason}</TooltipContent>
        </Tooltip>
      ) : (
        <Button variant="ghost" size="icon-sm" aria-label={`Delete ${name}`} onClick={onDelete}>
          <Trash2Icon />
        </Button>
      )}
    </div>
  )
}
