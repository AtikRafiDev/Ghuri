import { useQuery, useQueryClient } from '@tanstack/react-query'
import { BanIcon, KeyRoundIcon, LockIcon, PlusIcon, ShieldCheckIcon, UserCheckIcon, UserCogIcon, UsersIcon, type LucideIcon } from 'lucide-react'
import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { Tooltip, TooltipContent, TooltipTrigger } from '@/components/ui/tooltip'
import { cn } from '@/lib/utils'
import { toAppError } from '@/shared/api/problem'
import { EmptyState } from '@/shared/components/EmptyState'
import { FormAlert } from '@/shared/components/FormAlert'
import { FormField } from '@/shared/components/FormField'
import { PageHeader } from '@/shared/components/PageHeader'
import { UserAvatar } from '@/shared/components/UserAvatar'
import { formatDateTime } from '@/shared/lib/dates'
import { ActionDialog } from '../../components/ActionDialog'
import {
  assignableStaffRoles,
  staffApi,
  staffKeys,
  staffRoleHints,
  staffRoleLabels,
  type StaffRole,
  type StaffUser,
} from '../api/staff.api'
import { StaffFormDialog } from '../components/StaffFormDialog'

const columnCount = 5

type Action = { kind: 'role' | 'disable' | 'enable' | 'link'; user: StaffUser }

/**
 * Admin → Staff (Super Admin only): the people who use the admin panel.
 * Customers sign up themselves; Manager, Sales and Accounts accounts are
 * only made here. A short list, so no paging.
 */
export function AdminStaffPage() {
  const queryClient = useQueryClient()
  const { data, isPending, isError, error, refetch } = useQuery({ queryKey: staffKeys.all, queryFn: staffApi.list })

  const [adding, setAdding] = useState(false)
  const [action, setAction] = useState<Action | null>(null)
  const [newRole, setNewRole] = useState<StaffRole>(3)

  const open = (kind: Action['kind'], user: StaffUser) => {
    if (kind === 'role') setNewRole(user.role === 1 ? 2 : user.role)
    setAction({ kind, user })
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: staffKeys.all })

  return (
    <div className="grid gap-6">
      <PageHeader
        title="Staff"
        description="Who can use the admin panel, and what they may do. Customers sign up on the website themselves."
        actions={
          <Button onClick={() => setAdding(true)}>
            <PlusIcon />
            Add staff member
          </Button>
        }
      />

      {isError && (
        <div className="grid justify-items-start gap-3">
          <FormAlert kind="error">{toAppError(error).message}</FormAlert>
          <Button variant="outline" size="sm" onClick={() => refetch()}>
            Try again
          </Button>
        </div>
      )}

      {data?.length === 0 ? (
        <EmptyState icon={UsersIcon} title="No staff yet" text="Add the people who run bookings, quotes and payments with you.">
          <Button onClick={() => setAdding(true)}>
            <PlusIcon />
            Add staff member
          </Button>
        </EmptyState>
      ) : (
        (isPending || data) && (
          <div className="animate-fade-up overflow-hidden rounded-2xl bg-card shadow-card ring-1 ring-ink-200/80">
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Name</TableHead>
                  <TableHead>Role</TableHead>
                  <TableHead>Status</TableHead>
                  <TableHead>Last login</TableHead>
                  <TableHead className="text-right">
                    <span className="sr-only">Actions</span>
                  </TableHead>
                </TableRow>
              </TableHeader>
              <TableBody aria-busy={isPending || undefined}>
                {/* First load: rows of shimmer in the table's own shape. */}
                {isPending &&
                  Array.from({ length: 4 }, (_, i) => (
                    <TableRow key={i} className="hover:bg-transparent">
                      <TableCell>
                        <div className="flex items-center gap-3">
                          <Skeleton className="size-9 rounded-full" />
                          <div className="grid gap-2">
                            <Skeleton className="h-4 w-32" />
                            <Skeleton className="h-3 w-44" />
                          </div>
                        </div>
                      </TableCell>
                      {Array.from({ length: columnCount - 1 }, (_, j) => (
                        <TableCell key={j}>
                          <Skeleton className={cn('h-6 rounded-full', j === 3 ? 'ml-auto w-28' : 'w-20')} />
                        </TableCell>
                      ))}
                    </TableRow>
                  ))}

                {data?.map((u) => (
                  // A disabled account fades back - all but its actions, which stay crisp (Enable lives there).
                  <TableRow key={u.id} className={cn(u.status === 3 && '[&>td:not(:last-child)]:opacity-55')}>
                    <TableCell>
                      <div className="flex items-center gap-3">
                        <UserAvatar name={u.fullName} className={cn(u.status === 3 && 'grayscale')} />
                        <div className="grid">
                          <span className="font-semibold text-ink-900">{u.fullName}</span>
                          <span className="nums text-xs text-ink-500">
                            {u.email ?? u.phone}
                            {u.email && <span className="text-ink-400"> · {u.phone}</span>}
                          </span>
                        </div>
                      </div>
                    </TableCell>
                    <TableCell>
                      {u.role === 1 ? (
                        <Badge variant="secondary">
                          <ShieldCheckIcon />
                          {staffRoleLabels[u.role]}
                        </Badge>
                      ) : (
                        <Badge variant="outline">{staffRoleLabels[u.role]}</Badge>
                      )}
                    </TableCell>
                    <TableCell>
                      {u.status === 3 ? (
                        <Badge variant="danger" dot>
                          Disabled
                        </Badge>
                      ) : u.passwordSet ? (
                        <Badge variant="success" dot>
                          Active
                        </Badge>
                      ) : (
                        <Badge variant="warning" dot title="They haven't used the welcome link yet.">
                          Invite pending
                        </Badge>
                      )}
                    </TableCell>
                    <TableCell className={u.lastLoginUtc ? 'text-ink-700' : 'text-ink-400'}>{u.lastLoginUtc ? formatDateTime(u.lastLoginUtc) : 'Never'}</TableCell>
                    <TableCell className="text-right">
                      {u.editable ? (
                        <div className="flex justify-end gap-1">
                          {u.status !== 3 ? (
                            <>
                              <IconAction icon={UserCogIcon} label="Change role" name={u.fullName} onClick={() => open('role', u)} />
                              <IconAction icon={KeyRoundIcon} label="Send password link" name={u.fullName} onClick={() => open('link', u)} />
                              <IconAction icon={BanIcon} label="Disable" name={u.fullName} danger onClick={() => open('disable', u)} />
                            </>
                          ) : (
                            <Button variant="outline" size="sm" onClick={() => open('enable', u)}>
                              <UserCheckIcon />
                              Enable
                            </Button>
                          )}
                        </div>
                      ) : (
                        <span className="inline-flex items-center gap-1.5 text-xs font-medium text-ink-400">
                          <LockIcon className="size-3.5" />
                          Owner account
                        </span>
                      )}
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        )
      )}

      <StaffFormDialog open={adding} onOpenChange={setAdding} />

      <ActionDialog
        open={action?.kind === 'role'}
        onOpenChange={(o) => !o && setAction(null)}
        title={`Change ${action?.user.fullName ?? ''}'s role`}
        description="Takes effect at their next login, or within 15 minutes."
        submitLabel="Change role"
        successMessage="Role changed"
        successDescription={action ? `${action.user.fullName} is now ${staffRoleLabels[newRole]}.` : undefined}
        onSubmit={async () => {
          if (!action) return
          await staffApi.changeRole(action.user.id, newRole)
          await refresh()
        }}
      >
        <FormField label="Role" htmlFor="new-role" hint={staffRoleHints[newRole]}>
          <Select value={String(newRole)} onValueChange={(v) => setNewRole(Number(v) as StaffRole)}>
            <SelectTrigger id="new-role" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent position="popper">
              {assignableStaffRoles.map((role) => (
                <SelectItem key={role} value={String(role)}>
                  {staffRoleLabels[role]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
        </FormField>
      </ActionDialog>

      <ActionDialog
        open={action?.kind === 'disable'}
        onOpenChange={(o) => !o && setAction(null)}
        title={`Disable ${action?.user.fullName ?? ''}?`}
        description="They are logged out and can't log in until you enable the account again. Nothing is deleted - their name stays on the bookings they handled."
        submitLabel="Disable"
        destructive
        successMessage="Account disabled"
        successDescription={action ? `${action.user.fullName} can no longer log in.` : undefined}
        onSubmit={async () => {
          if (!action) return
          await staffApi.disable(action.user.id)
          await refresh()
        }}
      >
        {null}
      </ActionDialog>

      <ActionDialog
        open={action?.kind === 'enable'}
        onOpenChange={(o) => !o && setAction(null)}
        title={`Enable ${action?.user.fullName ?? ''}?`}
        description="They can log in again with the password they had."
        submitLabel="Enable"
        successMessage="Account enabled"
        successDescription={action ? `${action.user.fullName} can log in again.` : undefined}
        onSubmit={async () => {
          if (!action) return
          await staffApi.enable(action.user.id)
          await refresh()
        }}
      >
        {null}
      </ActionDialog>

      <ActionDialog
        open={action?.kind === 'link'}
        onOpenChange={(o) => !o && setAction(null)}
        title="Send a password link?"
        description={`${action?.user.fullName ?? ''} gets an email at ${action?.user.email ?? 'their address'} with a new link to set their password. Older links stop working.`}
        submitLabel="Send link"
        successMessage="Password link sent"
        successDescription={action ? `Sent to ${action.user.email ?? `${action.user.fullName}'s email`}.` : undefined}
        onSubmit={async () => {
          if (!action) return
          await staffApi.sendPasswordLink(action.user.id)
        }}
      >
        {null}
      </ActionDialog>
    </div>
  )
}

/** One icon button at the end of a staff row, its words in a tooltip and for screen readers ("Disable Kamal Hasan"). */
function IconAction({ icon: Icon, label, name, danger, onClick }: { icon: LucideIcon; label: string; name: string; danger?: boolean; onClick: () => void }) {
  return (
    <Tooltip>
      <TooltipTrigger asChild>
        <Button
          variant="ghost"
          size="icon-sm"
          aria-label={`${label}: ${name}`}
          onClick={onClick}
          className={cn('text-ink-400', danger ? 'hover:bg-clay-50 hover:text-clay-600' : 'hover:bg-forest-50 hover:text-forest-700')}
        >
          <Icon />
        </Button>
      </TooltipTrigger>
      <TooltipContent>{label}</TooltipContent>
    </Tooltip>
  )
}
