import { useQuery } from '@tanstack/react-query'
import { CloudOffIcon, LuggageIcon } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { packagesQuery } from '@/features/catalog/api/catalog.api'
import { PackageCard, PackageCardSkeleton } from '@/features/catalog/components/PackageCard'
import { EmptyState } from '@/shared/components/EmptyState'
import { SectionHeading } from '@/shared/components/SectionHeading'
import { Reveal } from '@/shared/motion/Reveal'
import { popularPackagesSearch } from '../data/homeQueries'

/** Featured first, then the newest - 8 cards that rise in, one after another, as they scroll into view. */
export function PopularPackages() {
  const { data, isPending, isError, refetch } = useQuery(packagesQuery(popularPackagesSearch))

  return (
    <section className="mx-auto grid max-w-6xl gap-10 px-4 pb-20 sm:pb-28">
      <SectionHeading
        eyebrow="Hand-picked"
        title="Popular packages"
        text="Featured trips first, then the newest - group departures and flexible stays."
        link={{ to: '/packages', label: 'See all packages' }}
      />

      {isError && (
        <EmptyState icon={CloudOffIcon} title="Packages couldn't be loaded" text="Check your connection and try again.">
          <Button variant="outline" onClick={() => refetch()}>
            Try again
          </Button>
        </EmptyState>
      )}

      {data?.items.length === 0 && (
        <EmptyState icon={LuggageIcon} title="New packages are on their way" text="Check back soon - or tell us where you'd like to go." />
      )}

      {/* key: when the real cards replace the skeletons, the box starts over and they play their entrance. */}
      <Reveal key={data ? 'cards' : 'loading'} className="grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
        {isPending && Array.from({ length: 4 }, (_, i) => <PackageCardSkeleton key={i} />)}
        {data?.items.map((pkg) => <PackageCard key={pkg.id} pkg={pkg} />)}
      </Reveal>
    </section>
  )
}
