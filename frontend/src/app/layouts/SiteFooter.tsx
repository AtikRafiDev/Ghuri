import { ArrowRightIcon, LockIcon, MailIcon, MapPinIcon, PhoneIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { BrandLink } from '@/shared/components/BrandMark'
import { site } from '@/shared/config/site'

const exploreLinks = [
  { to: '/packages', label: 'All packages' },
  { to: '/packages?mode=FlexibleStay', label: 'Flexible stays' },
  { to: '/plan-trip', label: 'Plan a custom trip' },
]

// The information pages (Day 16, features/pages). SSLCommerz asks for these
// pages before approving a live merchant account.
const policyLinks = [
  { to: '/about', label: 'About us' },
  { to: '/faq', label: 'FAQ' },
  { to: '/terms', label: 'Terms & conditions' },
  { to: '/privacy', label: 'Privacy policy' },
  { to: '/refund-policy', label: 'Refund policy' },
]

const linkClass = 'w-fit text-forest-100/75 transition-colors hover:text-white'

/** The bottom of every public page: a "plan your own trip" invitation, then who we are, how to reach us, the policies. */
export function SiteFooter() {
  return (
    <footer className="brand-surface relative mt-16 overflow-hidden bg-forest-950 text-forest-100 print:hidden">
      <div aria-hidden className="bg-topo absolute inset-0 opacity-70" />
      <div className="relative mx-auto grid max-w-6xl gap-12 px-4 pt-14 pb-8">
        <div className="flex flex-col items-start justify-between gap-6 rounded-3xl bg-primary-hover/60 p-6 ring-1 ring-white/10 sm:flex-row sm:items-center sm:p-8">
          <div className="grid gap-1.5">
            <h2 className="text-xl font-bold text-white sm:text-2xl">Can't find the perfect package?</h2>
            <p className="text-sm text-forest-100/75 sm:text-base">Tell us where you want to go - we'll plan the route, hotels and transfers for you.</p>
          </div>
          <Button asChild variant="accent" size="lg">
            <Link to="/plan-trip">
              Plan my trip
              <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
            </Link>
          </Button>
        </div>

        <div className="grid gap-10 sm:grid-cols-2 lg:grid-cols-[1.4fr_1fr_1fr_1.2fr]">
          <div className="grid content-start gap-3">
            <BrandLink tone="light" />
            <p className="max-w-xs text-sm leading-relaxed text-forest-100/70">{site.tagline}. Hand-picked hotels, local guides and every transfer arranged.</p>
          </div>

          <FooterColumn title="Explore">
            {exploreLinks.map((link) => (
              <Link key={link.to} to={link.to} className={linkClass}>
                {link.label}
              </Link>
            ))}
          </FooterColumn>

          <FooterColumn title="Information" label="Policies">
            {policyLinks.map((link) => (
              <Link key={link.to} to={link.to} className={linkClass}>
                {link.label}
              </Link>
            ))}
          </FooterColumn>

          <FooterColumn title="Contact">
            <a href={`tel:${site.phone.replace(/\s/g, '')}`} className={`flex items-center gap-2.5 ${linkClass}`}>
              <PhoneIcon className="size-4 text-forest-400" />
              {site.phone}
            </a>
            <a href={`mailto:${site.email}`} className={`flex items-center gap-2.5 ${linkClass}`}>
              <MailIcon className="size-4 text-forest-400" />
              {site.email}
            </a>
            <span className="flex items-center gap-2.5 text-forest-100/75">
              <MapPinIcon className="size-4 text-forest-400" />
              {site.address}
            </span>
          </FooterColumn>
        </div>

        <div className="flex flex-col items-center justify-between gap-3 border-t border-white/10 pt-6 text-xs text-forest-100/60 sm:flex-row">
          <span>
            © {new Date().getFullYear()} {site.name}. All rights reserved.
          </span>
          <span className="flex items-center gap-1.5">
            <LockIcon className="size-3.5 text-forest-400" />
            Secure online payments by SSLCommerz
          </span>
        </div>
      </div>
    </footer>
  )
}

function FooterColumn({ title, label, children }: { title: string; label?: string; children: ReactNode }) {
  return (
    <nav aria-label={label ?? title} className="grid content-start gap-3 text-sm">
      <h2 className="text-xs font-semibold tracking-wider text-white uppercase">{title}</h2>
      {children}
    </nav>
  )
}
