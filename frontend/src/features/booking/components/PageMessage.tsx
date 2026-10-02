import type { ReactNode } from 'react'

/** A whole-page message with an action under it: "Nothing to book yet", "Your seats were released"... */
export function PageMessage({ title, text, children }: { title: string; text: ReactNode; children?: ReactNode }) {
  return (
    <section className="grid justify-items-center gap-3 py-16 text-center">
      <h1 className="text-2xl font-semibold">{title}</h1>
      <p className="max-w-md text-muted-foreground">{text}</p>
      {children && <div className="mt-2 flex flex-wrap justify-center gap-2">{children}</div>}
    </section>
  )
}
