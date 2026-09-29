import type { ReactNode } from 'react'
import { Card, CardContent, CardDescription, CardFooter, CardHeader, CardTitle } from '@/components/ui/card'

/** The centred card every auth page sits in - one look for login, register, forgot and reset. */
export function AuthCard({
  title,
  description,
  footer,
  children,
}: {
  title: string
  description?: string
  footer?: ReactNode
  children: ReactNode
}) {
  return (
    <div className="mx-auto w-full max-w-sm py-6">
      <Card>
        <CardHeader>
          <CardTitle className="text-xl">{title}</CardTitle>
          {description && <CardDescription>{description}</CardDescription>}
        </CardHeader>
        <CardContent>{children}</CardContent>
        {footer && <CardFooter className="justify-center text-sm text-muted-foreground">{footer}</CardFooter>}
      </Card>
    </div>
  )
}
