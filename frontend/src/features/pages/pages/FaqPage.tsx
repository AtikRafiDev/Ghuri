import { ChevronDownIcon, CircleHelpIcon, MessageCircleIcon } from 'lucide-react'
import type { ReactNode } from 'react'
import { Link } from 'react-router'
import { site, whatsAppLink } from '@/shared/config/site'
import { StaticPage } from '../components/StaticPage'

/** The questions customers ask most - each answer from how the website really works. */
const questions: { q: string; a: ReactNode }[] = [
  {
    q: 'How do I book a tour?',
    a: (
      <>
        Open a package, choose a date (or, for a flexible stay, your start date and nights) and the number of travellers, then click
        Book now. You'll enter the travellers' names and pay online.
      </>
    ),
  },
  {
    q: 'How can I pay?',
    a: 'Online through SSLCommerz with bKash, Nagad, Rocket, or a Visa / Mastercard / Amex card. You can also pay at our office.',
  },
  {
    q: 'Why is there a 20-minute countdown when I book?',
    a: "We hold your seats while you pay. If you don't pay within 20 minutes, the seats go back on sale and you can book again.",
  },
  {
    q: 'When do I get my voucher?',
    a: "Right after your payment is confirmed - usually within a minute. It's emailed to you together with your invoice, and you can download both any time under My bookings.",
  },
  {
    q: 'What is a flexible stay?',
    a: 'A package where you choose the start date and how many nights to stay. The price depends on the nights. Flexible stays are confirmed with the hotel within 24 hours.',
  },
  {
    q: 'Can you plan a trip to several places for me?',
    a: (
      <>
        Yes - use <Link to="/plan-trip">Plan my trip</Link>. Add the places in order with the nights at each, and we send you a price,
        usually within 24 hours. It's free and without obligation.
      </>
    ),
  },
  {
    q: 'Can I cancel? Will I get my money back?',
    a: (
      <>
        Yes, under My bookings, until the day before the trip. How much comes back depends on how early you cancel - see our{' '}
        <Link to="/refund-policy">refund policy</Link>. The page shows the exact amount before you confirm.
      </>
    ),
  },
  {
    q: 'I paid but my booking still says "waiting for payment".',
    a: (
      <>
        Payments are usually confirmed within a minute. If it takes longer, don't pay again -{' '}
        <a href={whatsAppLink('Hello, I paid but my booking still shows waiting for payment.')} target="_blank" rel="noopener noreferrer">
          message us on WhatsApp
        </a>{' '}
        with your booking number. If you ever pay twice, the extra payment is refunded in full.
      </>
    ),
  },
  {
    q: 'Do children and infants pay?',
    a: 'Children pay the child price shown on the package (for flexible stays, the adult price). Infants under 2 travel free on an adult’s lap.',
  },
]

/**
 * /faq - questions and answers as an accordion: each one opens on click (no
 * JavaScript needed: details/summary). Hairlines between the questions; the
 * open one's chevron turns over and goes green.
 */
export function FaqPage() {
  return (
    <StaticPage title="Frequently asked questions" description={`Answers about booking, paying and cancelling with ${site.name}.`} updated="6 October 2026" icon={CircleHelpIcon} draft>
      <div className="-mt-3 grid divide-y divide-ink-100">
        {questions.map(({ q, a }) => (
          <details key={q} className="group">
            <summary className="flex cursor-pointer list-none items-center justify-between gap-4 rounded-xl py-4 text-base leading-snug font-semibold text-ink-900 transition-colors hover:text-forest-700 focus-visible:ring-4 focus-visible:ring-ring/25 focus-visible:outline-none [&::-webkit-details-marker]:hidden">
              {q}
              <span className="flex size-8 shrink-0 items-center justify-center rounded-full bg-ink-50 text-ink-500 ring-1 ring-ink-200 transition-[rotate,background-color,color] duration-300 group-open:rotate-180 group-open:bg-forest-50 group-open:text-forest-700 group-open:ring-forest-200">
                <ChevronDownIcon className="size-4" />
              </span>
            </summary>
            <p className="animate-fade-in pr-12 pb-5 text-ink-600">{a}</p>
          </details>
        ))}
      </div>
      <div className="mt-2 flex items-center gap-4 rounded-2xl bg-forest-50 p-4 ring-1 ring-forest-100 sm:p-5">
        <span className="flex size-11 shrink-0 items-center justify-center rounded-xl bg-card text-forest-600 shadow-soft">
          <MessageCircleIcon className="size-5" />
        </span>
        <p className="text-ink-700">
          Still have a question? Call {site.phone} or{' '}
          <a href={whatsAppLink()} target="_blank" rel="noopener noreferrer">
            chat with us on WhatsApp
          </a>
          .
        </p>
      </div>
    </StaticPage>
  )
}
