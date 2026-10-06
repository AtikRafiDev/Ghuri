# Ghuri – Acceptance test checklist (UAT)

Please try each step below and tick it when it works as described. If something
doesn't, write it in the **Problems found** table at the end: what you did, what
you expected, and what happened (a screenshot helps).

- **Website:** http://localhost:5173 (on the developer's PC)
- **Test emails:** every email the website sends appears at http://localhost:5000. Nothing reaches a real inbox.
- **Test payments:** SSLCommerz **sandbox**. Use its test cards / test bKash; no real money moves.
- **Accounts:** a customer account (register one) and a staff account (ask the developer).

---

## 1. Browsing (no login needed)

- [ ] The home page shows packages and destinations.
- [ ] **Packages**: search by name, filter by destination and price, sort by price. The list and page numbers update.
- [ ] A package page shows photos, the day-by-day plan, what's included, and a price.
- [ ] A **fixed-date** package lists its dates with seats left; a **flexible stay** lets you pick a start date and nights, and the price changes with the nights.
- [ ] The footer links open **About, FAQ, Terms, Privacy, Refund policy**. (They are marked *Draft* - check the wording.)
- [ ] The green **WhatsApp** button (bottom right) opens WhatsApp. *(It uses a placeholder number for now.)*

## 2. Account

- [ ] Register with a mobile number, password and email. You are logged in.
- [ ] Log out and log in again (mobile number or email).
- [ ] **Forgot password**: the email arrives (at localhost:5000), the link sets a new password, and the old one stops working.
- [ ] **My account → Profile**: change your name. Changing your email asks for your password.

## 3. Book a fixed-date package

- [ ] Choose a date and travellers → **Book now** → enter every traveller's name → continue.
- [ ] The payment page shows a **20-minute countdown** and the price.
- [ ] Pay in the SSLCommerz sandbox. You return to "Payment received - you're booked!"
- [ ] Within a minute, a **"Booking confirmed"** email arrives with the **e-voucher** and **invoice** PDFs. Open both: names, dates and amounts are right.
- [ ] **My bookings** shows the booking as **Confirmed**; both PDFs download from there.
- [ ] The package page shows fewer seats left on that date.

## 4. Book a flexible stay

- [ ] Choose a start date and nights → book → pay as above.
- [ ] The voucher shows the check-in and check-out dates and the nights.

## 5. Don't pay

- [ ] Book, then leave the payment page without paying. After 20 minutes the booking shows **Expired** and the seats are back on sale.
- [ ] With 3 unpaid bookings open at once, a 4th is refused with a clear message.

## 6. Cancel

- [ ] Cancel an **unpaid** booking: no money involved; the seats come back.
- [ ] Cancel a **paid** booking: **before confirming**, the page shows how much comes back (by the refund policy). After cancelling, the booking shows the refund as "requested".

## 7. Custom trip (Plan my trip)

- [ ] **Plan my trip**: add Cox's Bazar 3 nights → Sylhet 2 nights. The dates of each stop update as you type. Send.
- [ ] Two emails: one to the agency (bookings@…), one to the customer.
- [ ] *Staff:* **Admin → Custom trips** shows the request under *Waiting*. Open it, add price lines, **Send quote**.
- [ ] *Customer:* the quote email arrives. **My trips** shows the quote with a countdown. **Accept & pay** → enter names → pay in the sandbox.
- [ ] The trip shows **Paid**. The voucher email lists **every stop** with its dates.
- [ ] A quote past its validity can't be accepted.

## 8. Staff (admin panel)

- [ ] **Dashboard** shows today's bookings, revenue, bookings waiting for payment, refunds to process, upcoming trips.
- [ ] **Bookings**: find a booking by number, name or mobile. Open it: travellers, history, payments.
- [ ] **Record payment** (cash / bank / bKash) on an unpaid booking: it becomes Confirmed and the voucher email goes out.
- [ ] **Cancel booking** as the agency: the customer gets a **full** refund request.
- [ ] **Refunds**: mark a refund as sent with a transaction id; it moves out of *To process*.
- [ ] A **Sales** account cannot record payments or mark refunds; an **Accounts** account cannot cancel bookings.

## 9. On a phone

Open the website on a phone (or in Chrome: F12 → the phone icon → width **360**).

- [ ] Home, packages, a package page, checkout, My bookings and Plan my trip are readable and usable at 360 px wide, with no sideways scrolling.
- [ ] Buttons are easy to tap; the WhatsApp button doesn't cover anything important.

---

## Problems found

| # | Page / step | What I did | What I expected | What happened |
|---|---|---|---|---|
| 1 | | | | |
| 2 | | | | |
| 3 | | | | |

**Tested by:** ______________ **Date:** __________ **Approved for go-live:** ☐ Yes ☐ Not yet
