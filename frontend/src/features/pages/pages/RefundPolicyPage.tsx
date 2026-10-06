import { ReceiptTextIcon } from 'lucide-react'
import { Link } from 'react-router'
import { site } from '@/shared/config/site'
import { StaticPage } from '../components/StaticPage'

/**
 * /refund-policy - written from the rules the system really applies
 * (backend: CancellationTerms + the seeded global CancellationPolicy rows,
 * CancelBookingByAgency, the late/double payment refunds). If those rules
 * change, change this page in the same commit.
 */
export function RefundPolicyPage() {
  return (
    <StaticPage
      title="Cancellation & refund policy"
      description={`How cancelling a ${site.name} booking works, and how much you get back.`}
      updated="6 October 2026"
      icon={ReceiptTextIcon}
      draft
    >
      <p>
        We know plans change. This page explains what happens when you cancel a booking, when we cancel one, and how the money
        comes back to you.
      </p>

      <h2>If you cancel</h2>
      <p>
        You can cancel any booking yourself under <Link to="/account/bookings">My bookings</Link> until the day before your trip
        starts. Before you confirm, the page shows exactly how much you will get back. From the first day of the trip, please call
        or WhatsApp us.
      </p>
      <table>
        <thead>
          <tr>
            <th>When you cancel</th>
            <th>You get back</th>
          </tr>
        </thead>
        <tbody>
          <tr>
            <td>30 days or more before the trip starts</td>
            <td>100% of what you paid</td>
          </tr>
          <tr>
            <td>15 to 29 days before</td>
            <td>50%</td>
          </tr>
          <tr>
            <td>7 to 14 days before</td>
            <td>25%</td>
          </tr>
          <tr>
            <td>Less than 7 days before</td>
            <td>No refund</td>
          </tr>
        </tbody>
      </table>
      <p>
        Days are counted in Bangladesh dates: cancelling on 17 December for a trip that starts on 20 December is 3 days before.
      </p>
      <p>A booking you have not paid for yet can be cancelled at any time, free of charge.</p>

      <h2>If we cancel</h2>
      <p>
        Sometimes we have to cancel - a departure doesn't run, or the hotel for a flexible stay can't be confirmed. Then you get{' '}
        <strong>100% of what you paid</strong> back, whatever the date.
      </p>

      <h2>Payments that arrive late or twice</h2>
      <p>
        You have 20 minutes to pay after booking. If your payment reaches us after that and the seats have been given to someone
        else, or if you accidentally pay twice, we refund that payment in full.
      </p>

      <h2>How the money comes back</h2>
      <ul>
        <li>We send it back the way you paid where we can - for example to the same bKash or Nagad number, or your bank account.</li>
        <li>We aim to send refunds within 7 working days of the cancellation. Your bank or wallet may take a few more days to show it.</li>
        <li>You can follow the refund on your booking's page under My bookings.</li>
        <li>Gateway charges are not deducted from your refund.</li>
      </ul>

      <h2>Custom trips</h2>
      <p>
        Asking for a custom trip quote is free and without obligation. Once you accept a quote and pay, the trip is a booking and
        this policy applies to it in the same way.
      </p>

      <h2>Questions</h2>
      <p>
        Call us on {site.phone}, email <a href={`mailto:${site.email}`}>{site.email}</a>, or chat with us on WhatsApp.
      </p>
    </StaticPage>
  )
}
