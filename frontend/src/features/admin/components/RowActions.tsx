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

/** The Edit / Delete buttons at the end of an admin table row: green on hover to edit, terracotta to delete. */
export function RowActions({ name, onEdit, onDelete, deleteBlockedReason }: RowActionsProps) {
  return (
    <div className="flex justify-end gap-1">
      <Tooltip>
        <TooltipTrigger asChild>
          <Button variant="ghost" size="icon-sm" aria-label={`Edit ${name}`} onClick={onEdit} className="text-ink-400 hover:bg-forest-50 hover:text-forest-700">
            <PencilIcon />
          </Button>
        </TooltipTrigger>
        <TooltipContent>Edit</TooltipContent>
      </Tooltip>
      {deleteBlockedReason ? (
        <Tooltip>
          <TooltipTrigger asChild>
            {/* A disabled button gets no mouse or focus events, so the tooltip
                hangs on a focusable wrapper - keyboard users can reach it too. */}
            <span tabIndex={0} className="rounded-lg">
              <Button variant="ghost" size="icon-sm" disabled aria-label={`Delete ${name}`} className="text-ink-300">
                <Trash2Icon />
              </Button>
            </span>
          </TooltipTrigger>
          <TooltipContent>{deleteBlockedReason}</TooltipContent>
        </Tooltip>
      ) : (
        <Tooltip>
          <TooltipTrigger asChild>
            <Button variant="ghost" size="icon-sm" aria-label={`Delete ${name}`} onClick={onDelete} className="text-ink-400 hover:bg-clay-50 hover:text-clay-600">
              <Trash2Icon />
            </Button>
          </TooltipTrigger>
          <TooltipContent>Delete</TooltipContent>
        </Tooltip>
      )}
    </div>
  )
}
