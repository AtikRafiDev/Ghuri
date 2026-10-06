import { MailIcon, MapPinIcon, PhoneIcon } from 'lucide-react'
import { Link } from 'react-router'
import { site } from '@/shared/config/site'

// The information pages (Day 16, features/pages). SSLCommerz asks for these
// pages before approving a live merchant account.
const policyLinks = [
  { to: '/about', label: 'About us' },
  { to: '/faq', label: 'FAQ' },
  { to: '/terms', label: 'Terms & conditions' },
  { to: '/privacy', label: 'Privacy policy' },
  { to: '/refund-policy', label: 'Refund policy' },
]

/** The bottom of every public page: who we are, how to reach us, the policies. */
export function SiteFooter() {
  return (
    <footer className="border-t bg-muted/30">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-10 sm:grid-cols-3">
        <div className="grid content-start gap-2">
          <Link to="/" className="text-lg font-semibold tracking-tight">
            {site.name}
          </Link>
          <p className="text-sm text-muted-foreground">{site.tagline}.</p>
        </div>

        <div className="grid content-start gap-2 text-sm">
          <h2 className="font-medium">Contact</h2>
          <a href={`tel:${site.phone.replace(/\s/g, '')}`} className="flex items-center gap-2 text-muted-foreground hover:text-foreground">
            <PhoneIcon className="size-4" />
            {site.phone}
          </a>
          <a href={`mailto:${site.email}`} className="flex items-center gap-2 text-muted-foreground hover:text-foreground">
            <MailIcon className="size-4" />
            {site.email}
          </a>
          <span className="flex items-center gap-2 text-muted-foreground">
            <MapPinIcon className="size-4" />
            {site.address}
          </span>
        </div>

        <nav className="grid content-start gap-2 text-sm" aria-label="Policies">
          <h2 className="font-medium">Information</h2>
          {policyLinks.map((link) => (
            <Link key={link.to} to={link.to} className="text-muted-foreground hover:text-foreground">
              {link.label}
            </Link>
          ))}
        </nav>
      </div>
      <div className="border-t py-4 text-center text-xs text-muted-foreground">
        © {new Date().getFullYear()} {site.name}. All rights reserved.
      </div>
    </footer>
  )
}
