import { ArrowRightIcon, CompassIcon } from 'lucide-react'
import { Link } from 'react-router'
import { Button } from '@/components/ui/button'
import { site } from '@/shared/config/site'
import { StaticPage } from '../components/StaticPage'

/** /about - a draft frame; the agency's own story, team and licence numbers come from the client. */
export function AboutPage() {
  return (
    <StaticPage title={`About ${site.name}`} description={`${site.name} - ${site.tagline}.`} updated="6 October 2026" icon={CompassIcon} draft>
      <p className="text-lg leading-8 text-ink-700">
        {site.name} plans tours across Bangladesh and beyond - from a weekend at Cox's Bazar to a trip through several cities, planned
        around you.
      </p>

      <h2>What we do</h2>
      <ul>
        <li>
          <strong>Group tours on set dates</strong> - book a seat, travel with others, everything arranged.
        </li>
        <li>
          <strong>Flexible stays</strong> - choose your own start date and how many nights.
        </li>
        <li>
          <strong>Custom trips</strong> - tell us the places you want to see and we plan the hotels and transport and send you a price.
        </li>
      </ul>

      <h2>Why travel with us</h2>
      <ul>
        <li>Clear prices in Taka - what you see before paying is what you pay.</li>
        <li>Pay safely with bKash, Nagad or your card; your voucher arrives by email straight away.</li>
        <li>Fair cancellation rules, shown before you cancel.</li>
        <li>Real people to help you - by phone, email or WhatsApp.</li>
      </ul>

      {/* ⚠ CLIENT CONTENT: the agency's story, founding year, team, trade licence and tourism board registration numbers. */}
      <h2>Our story</h2>
      <p className="text-ink-400 italic">[The agency's own story, team and registration details go here.]</p>

      <div className="mt-3 flex flex-wrap gap-3 border-t border-ink-100 pt-7">
        <Button asChild>
          <Link to="/packages">
            See our packages
            <ArrowRightIcon className="group-hover/button:translate-x-0.5" />
          </Link>
        </Button>
        <Button asChild variant="outline">
          <Link to="/plan-trip">Plan my trip</Link>
        </Button>
      </div>
    </StaticPage>
  )
}
