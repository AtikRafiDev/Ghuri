import { ShieldCheckIcon } from 'lucide-react'
import { Link } from 'react-router'
import { site } from '@/shared/config/site'
import { StaticPage } from '../components/StaticPage'

/**
 * /privacy - written from what the system really stores (iam.Users, bookings
 * and travellers, payments, notifications, the refresh-token cookie, request
 * logs). If we start collecting something new, add it here in the same commit.
 */
export function PrivacyPage() {
  return (
    <StaticPage
      title="Privacy policy"
      description={`What personal data ${site.name} keeps, why, and your choices.`}
      updated="6 October 2026"
      icon={ShieldCheckIcon}
      draft
    >
      <p>
        This page explains what we keep about you when you use the {site.name} website, why, and who we share it with. We keep only
        what we need to arrange your trips.
      </p>

      <h2>What we keep</h2>
      <ul>
        <li>
          <strong>Your account:</strong> your name, mobile number, email (optional) and password. Your password is stored scrambled
          (hashed) - nobody, including us, can read it.
        </li>
        <li>
          <strong>Your bookings and trip requests:</strong> the trips, dates, the names of the people travelling, a contact number and
          email, and any special requests you write.
        </li>
        <li>
          <strong>Payments:</strong> the amount, the method (for example bKash) and the payment's reference. Card numbers and wallet
          PINs are entered on SSLCommerz's secure page - we never receive them.
        </li>
        <li>
          <strong>Technical data:</strong> your IP address and basic request details, kept in our logs to protect the website from abuse
          (for example, too many wrong passwords).
        </li>
      </ul>

      <h2>Why we use it</h2>
      <ul>
        <li>To make and manage your bookings, send your voucher and invoice, and handle cancellations and refunds.</li>
        <li>To contact you about your trip - by email, phone, SMS or WhatsApp.</li>
        <li>To keep your account and our website secure.</li>
        <li>Because the law requires it - for example, keeping records of payments.</li>
      </ul>
      <p>We do not sell your data, and we do not use it for advertising.</p>

      <h2>Who we share it with</h2>
      <ul>
        <li>The hotels, transport companies and guides who provide your trip - only what they need (names, dates, contact).</li>
        <li>SSLCommerz, to process your payment.</li>
        <li>Our email provider, to deliver our emails to you.</li>
        <li>Authorities, if the law requires us to.</li>
      </ul>

      <h2>Cookies</h2>
      <p>
        We use one cookie to keep you logged in (for up to 30 days, or until you log out). The checkout also remembers your
        progress in your browser until you close the tab. We don't use advertising or tracking cookies.
      </p>

      <h2>How long we keep it</h2>
      <p>
        We keep your account until you ask us to close it. Booking and payment records are kept for as long as the law requires for
        accounting, even after an account is closed.
      </p>

      <h2>Your choices</h2>
      <ul>
        <li>
          See and change your name and email under <Link to="/account/profile">My account</Link>.
        </li>
        <li>Ask us for a copy of your data, to correct it, or to close your account - contact us below.</li>
      </ul>

      <h2>Contact</h2>
      <p>
        Questions about your data: <a href={`mailto:${site.email}`}>{site.email}</a> · {site.phone} · {site.address}
      </p>
    </StaticPage>
  )
}
