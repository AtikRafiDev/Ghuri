import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router'
import { CategoryIcon } from '@/features/admin/categories/components/CategoryIcon'
import { categoriesQuery } from '@/features/catalog/api/catalog.api'
import { SectionHeading } from '@/shared/components/SectionHeading'
import { Reveal } from '@/shared/motion/Reveal'

/** Beach, Hill, Honeymoon... each opens the search filtered to it. The chips pop in one after another. */
export function Categories() {
  const { data } = useQuery(categoriesQuery)
  if (!data || data.length === 0) return null

  return (
    <section className="mx-auto grid max-w-6xl gap-10 px-4 pt-20 pb-4 sm:pt-28 sm:pb-8">
      <SectionHeading eyebrow="Trip styles" title="Find your kind of trip" text="Every trip is tagged by style - pick one to see them all." />
      <Reveal y={16} stagger={0.04} className="flex flex-wrap gap-3">
        {data.map((c) => (
          <Link
            key={c.id}
            to={`/packages?category=${encodeURIComponent(c.slug)}`}
            className="group inline-flex h-14 items-center gap-3 rounded-full bg-card pr-6 pl-2 text-[0.9375rem] font-semibold text-ink-700 shadow-soft ring-1 ring-ink-200 transition-[translate,box-shadow,color] duration-300 ease-(--ease-out-expo) hover:-translate-y-0.5 hover:text-forest-800 hover:shadow-card hover:ring-forest-300 focus-visible:ring-4 focus-visible:ring-ring/30 focus-visible:outline-none"
          >
            <span className="flex size-10 items-center justify-center rounded-full bg-forest-50 text-forest-600 transition-colors duration-300 group-hover:bg-primary group-hover:text-white">
              <CategoryIcon name={c.icon} className="size-[18px]" />
            </span>
            {c.name}
          </Link>
        ))}
      </Reveal>
    </section>
  )
}
