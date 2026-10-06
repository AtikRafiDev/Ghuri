import { Link } from 'react-router'
import { site } from '@/shared/config/site'
import { StaticPage } from '../components/StaticPage'

/** /terms - a draft written from how bookings actually work; to be reviewed by the client and a lawyer before go-live. */
export function TermsPage() {
  return (
    <StaticPage
      title="Terms & conditions"
      description={`The terms for booking tours with ${site.name}.`}
      updated="6 October 2026"
      draft
    >
      <p>
        These terms apply when you book a tour with {site.name} ("we", "us"), {site.address}. By booking you agree to them, so please
        read them before you pay.
      </p>

      <h2>1. Your account</h2>
      <ul>
        <li>You need an account to book. Keep your password private - you are responsible for bookings made with your account.</li>
        <li>Please give us a correct mobile number and email: your confirmation, voucher and invoice are sent there.</li>
      </ul>

      <h2>2. Prices</h2>
      <ul>
        <li>Prices are in Bangladeshi Taka (৳) and include what the package page lists as included.</li>
        <li>The price you see before paying is the price you pay. Price changes afterwards never change a booking you have made.</li>
        <li>Children pay as shown on each package; infants under 2 travel on an adult's lap.</li>
      </ul>

      <h2>3. Booking and payment</h2>
      <ul>
        <li>When you book, we hold your seats for 20 minutes. If you don't pay within that time, the booking expires and the seats are released.</li>
        <li>Payment is made online through SSLCommerz (bKash, Nagad, cards and more) or at our office. We never see your card number or wallet PIN.</li>
        <li>Your booking is confirmed when your payment is confirmed. We then email your e-voucher and invoice.</li>
        <li>
          <strong>Flexible stays</strong> (you choose the start date and nights) are subject to hotel confirmation within 24 hours. If we
          can't confirm, we cancel and refund you in full.
        </li>
      </ul>

      <h2>4. Custom trips</h2>
      <ul>
        <li>Asking for a custom trip is free. We send you a quote, usually within 24 hours.</li>
        <li>A quote is valid for the time shown on it (normally 3 days). After that, the price may change.</li>
        <li>When you accept a quote you give the names of everyone travelling, and pay within 20 minutes - like any booking.</li>
      </ul>

      <h2>5. Cancellations and refunds</h2>
      <p>
        See our <Link to="/refund-policy">cancellation &amp; refund policy</Link>.
      </p>

      <h2>6. Changes by us</h2>
      <p>
        If we must change an important part of your trip (dates, destination, hotel level), we tell you as soon as possible. You can
        accept the change or cancel for a full refund.
      </p>

      <h2>7. Your responsibilities</h2>
      <ul>
        <li>Bring a valid photo ID (and passport and visa for international trips) and show your e-voucher at the start of the trip.</li>
        <li>Be on time. If you miss a departure, we cannot refund the parts of the trip you miss.</li>
        <li>Follow the guide's safety instructions and local laws.</li>
      </ul>

      <h2>8. Our responsibility</h2>
      <p>
        We arrange your trip with care using hotels and transport we trust. We are not responsible for delays or losses caused by
        weather, natural disasters, strikes, government orders or other events outside our control, but we will help you as much as we can.
      </p>

      <h2>9. Law</h2>
      <p>These terms are governed by the laws of Bangladesh.</p>

      <h2>10. Contact</h2>
      <p>
        {site.name}, {site.address} · {site.phone} · <a href={`mailto:${site.email}`}>{site.email}</a>
      </p>
    </StaticPage>
  )
}
