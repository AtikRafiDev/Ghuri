import { useQuery, useQueryClient } from '@tanstack/react-query'
import { PlusIcon } from 'lucide-react'
import { useState } from 'react'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Label } from '@/components/ui/label'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@/components/ui/select'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { FormAlert } from '@/shared/components/FormAlert'
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
  const [notice, setNotice] = useState<string | null>(null)
  const [newRole, setNewRole] = useState<StaffRole>(3)

  const open = (kind: Action['kind'], user: StaffUser) => {
    setNotice(null)
    if (kind === 'role') setNewRole(user.role === 1 ? 2 : user.role)
    setAction({ kind, user })
  }

  const refresh = () => queryClient.invalidateQueries({ queryKey: staffKeys.all })

  return (
    <div className="grid gap-6">
      <div className="flex flex-wrap items-end justify-between gap-4">
        <div>
          <h1 className="text-2xl font-semibold">Staff</h1>
          <p className="text-muted-foreground">Who can use the admin panel, and what they may do. Customers sign up on the website themselves.</p>
        </div>
        <Button
          onClick={() => {
            setNotice(null)
            setAdding(true)
          }}
        >
          <PlusIcon />
          Add staff member
        </Button>
      </div>

      {notice && <FormAlert kind="success">{notice}</FormAlert>}

      <div className="rounded-lg border">
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
          <TableBody>
            {isPending &&
              Array.from({ length: 4 }, (_, i) => (
                <TableRow key={i}>
                  <TableCell colSpan={columnCount}>
                    <Skeleton className="h-8 w-full" />
                  </TableCell>
                </TableRow>
              ))}

            {isError && (
              <TableRow>
                <TableCell colSpan={columnCount} className="py-10 text-center">
                  <p className="text-destructive">{error.message}</p>
                  <Button variant="outline" size="sm" className="mt-3" onClick={() => refetch()}>
                    Try again
                  </Button>
                </TableCell>
              </TableRow>
            )}

            {data?.map((u) => (
              <TableRow key={u.id} className={u.status === 3 ? 'text-muted-foreground' : undefined}>
                <TableCell>
                  <div className="font-medium">{u.fullName}</div>
                  <div className="text-xs text-muted-foreground">
                    {u.phone}
                    {u.email && ` · ${u.email}`}
                  </div>
                </TableCell>
                <TableCell>
                  <Badge variant={u.role === 1 ? 'default' : 'secondary'}>{staffRoleLabels[u.role]}</Badge>
                </TableCell>
                <TableCell>
                  {u.status === 3 ? (
                    <Badge variant="destructive">Disabled</Badge>
                  ) : u.passwordSet ? (
                    <Badge variant="outline">Active</Badge>
                  ) : (
                    <Badge variant="outline" title="They haven't used the welcome link yet.">
                      Invite sent
                    </Badge>
                  )}
                </TableCell>
                <TableCell className="text-sm">{u.lastLoginUtc ? formatDateTime(u.lastLoginUtc) : 'Never'}</TableCell>
                <TableCell className="text-right">
                  {u.editable ? (
                    <div className="flex flex-wrap justify-end gap-1">
                      {u.status !== 3 && (
                        <>
                          <Button variant="ghost" size="sm" onClick={() => open('role', u)}>
                            Change role
                          </Button>
                          <Button variant="ghost" size="sm" onClick={() => open('link', u)}>
                            Send password link
                          </Button>
                          <Button variant="ghost" size="sm" className="text-destructive" onClick={() => open('disable', u)}>
                            Disable
                          </Button>
                        </>
                      )}
                      {u.status === 3 && (
                        <Button variant="ghost" size="sm" onClick={() => open('enable', u)}>
                          Enable
                        </Button>
                      )}
                    </div>
                  ) : (
                    <span className="text-xs text-muted-foreground">Owner account</span>
                  )}
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>

      <StaffFormDialog
        open={adding}
        onOpenChange={setAdding}
        onCreated={(email) => setNotice(`Staff member added. A link to set their password was emailed to ${email}.`)}
      />

      <ActionDialog
        open={action?.kind === 'role'}
        onOpenChange={(o) => !o && setAction(null)}
        title={`Change ${action?.user.fullName ?? ''}'s role`}
        description="Takes effect at their next login, or within 15 minutes."
        submitLabel="Change role"
        onSubmit={async () => {
          if (!action) return
          await staffApi.changeRole(action.user.id, newRole)
          await refresh()
          setNotice(`${action.user.fullName} is now ${staffRoleLabels[newRole]}.`)
        }}
      >
        <div className="grid gap-2">
          <Label htmlFor="new-role">Role</Label>
          <Select value={String(newRole)} onValueChange={(v) => setNewRole(Number(v) as StaffRole)}>
            <SelectTrigger id="new-role" className="w-full">
              <SelectValue />
            </SelectTrigger>
            <SelectContent>
              {assignableStaffRoles.map((role) => (
                <SelectItem key={role} value={String(role)}>
                  {staffRoleLabels[role]}
                </SelectItem>
              ))}
            </SelectContent>
          </Select>
          <p className="text-sm text-muted-foreground">{staffRoleHints[newRole]}</p>
        </div>
      </ActionDialog>

      <ActionDialog
        open={action?.kind === 'disable'}
        onOpenChange={(o) => !o && setAction(null)}
        title={`Disable ${action?.user.fullName ?? ''}?`}
        description="They are logged out and can't log in until you enable the account again. Nothing is deleted - their name stays on the bookings they handled."
        submitLabel="Disable"
        destructive
        onSubmit={async () => {
          if (!action) return
          await staffApi.disable(action.user.id)
          await refresh()
          setNotice(`${action.user.fullName} can no longer log in.`)
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
        onSubmit={async () => {
          if (!action) return
          await staffApi.enable(action.user.id)
          await refresh()
          setNotice(`${action.user.fullName} can log in again.`)
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
        onSubmit={async () => {
          if (!action) return
          await staffApi.sendPasswordLink(action.user.id)
          setNotice(`Password link sent to ${action.user.email}.`)
        }}
      >
        {null}
      </ActionDialog>
    </div>
  )
}
