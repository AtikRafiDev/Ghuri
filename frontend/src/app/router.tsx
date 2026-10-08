import { createBrowserRouter, Outlet } from 'react-router'
import { bookingDeskRoles, catalogueRoles, staffManagerRoles, staffRoles, systemHealthRoles } from '@/features/auth/auth.types'
import { RequireAuth } from '@/features/auth/components/RequireAuth'
import { RequireRole } from '@/features/auth/components/RequireRole'
import { AccountLayout } from './layouts/AccountLayout'
import { AdminLayout } from './layouts/AdminLayout'
import type { PublicRouteHandle } from './layouts/fullBleed'
import { PublicLayout } from './layouts/PublicLayout'
import { NotFoundPage } from './NotFoundPage'
import { RouteErrorPage } from './RouteErrorPage'

// Every page is LAZY: its code is downloaded only when first visited
// (blueprint 13.2: "route-level code splitting - admin bundles never load
// on the public site"). A customer's phone never downloads admin screens.

// Edge to edge, starting under the header: for the pages that open with a photo hero (layouts/fullBleed.ts).
const fullBleed = { fullBleed: true } satisfies PublicRouteHandle

export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      {
        index: true,
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/home/pages/HomePage')).HomePage }),
      },
      {
        path: 'packages',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/catalog/pages/PackagesPage')).PackagesPage }),
      },
      {
        path: 'packages/:slug',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/catalog/pages/PackageDetailsPage')).PackageDetailsPage }),
      },
      {
        // Login needed: a visitor goes to /login and comes back here with the trip still in the URL.
        path: 'checkout',
        element: (
          <RequireAuth>
            <Outlet />
          </RequireAuth>
        ),
        children: [
          // Step 1: traveller details → books the trip (seats held 20 minutes).
          { index: true, lazy: async () => ({ Component: (await import('@/features/booking/pages/CheckoutPage')).CheckoutPage }) },
          {
            // Step 2: countdown + "Pay" → SSLCommerz. A refresh reloads the booking from the API.
            path: ':bookingNo',
            lazy: async () => ({ Component: (await import('@/features/booking/pages/BookingPaymentPage')).BookingPaymentPage }),
          },
        ],
      },
      {
        // The custom trip builder (Day 14). Login first: the request belongs to an account,
        // and the quote is emailed to it. A visitor comes back here after logging in.
        path: 'plan-trip',
        handle: fullBleed,
        element: (
          <RequireAuth>
            <Outlet />
          </RequireAuth>
        ),
        children: [{ index: true, lazy: async () => ({ Component: (await import('@/features/trips/pages/PlanTripPage')).PlanTripPage }) }],
      },
      {
        // Back from SSLCommerz's page (the API's return address redirects here). No login needed to see it.
        path: 'payment/result',
        lazy: async () => ({ Component: (await import('@/features/booking/pages/PaymentResultPage')).PaymentResultPage }),
      },
      // Information pages (Day 16) - the footer links them; SSLCommerz asks for terms, privacy and refund policy before going live.
      { path: 'about', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/pages/pages/AboutPage')).AboutPage }) },
      { path: 'faq', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/pages/pages/FaqPage')).FaqPage }) },
      { path: 'terms', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/pages/pages/TermsPage')).TermsPage }) },
      { path: 'privacy', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/pages/pages/PrivacyPage')).PrivacyPage }) },
      {
        path: 'refund-policy',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/pages/pages/RefundPolicyPage')).RefundPolicyPage }),
      },
      {
        // The photographers of the site's own photos - their licences ask for the credit (shared/photos).
        path: 'photo-credits',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/pages/pages/PhotoCreditsPage')).PhotoCreditsPage }),
      },
      // The sign-in pages sit on a full-screen photo (auth/components/AuthCard).
      { path: 'login', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/auth/pages/LoginPage')).LoginPage }) },
      { path: 'register', handle: fullBleed, lazy: async () => ({ Component: (await import('@/features/auth/pages/RegisterPage')).RegisterPage }) },
      {
        path: 'forgot-password',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/auth/pages/ForgotPasswordPage')).ForgotPasswordPage }),
      },
      {
        // The address the reset EMAIL links to (API setting Auth:PasswordResetUrl).
        path: 'reset-password',
        handle: fullBleed,
        lazy: async () => ({ Component: (await import('@/features/auth/pages/ResetPasswordPage')).ResetPasswordPage }),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
  {
    // Any logged-in user.
    path: 'account',
    element: (
      <RequireAuth>
        <AccountLayout />
      </RequireAuth>
    ),
    errorElement: <RouteErrorPage />,
    children: [
      { index: true, lazy: async () => ({ Component: (await import('@/features/account/pages/AccountPage')).AccountPage }) },
      {
        path: 'bookings',
        lazy: async () => ({ Component: (await import('@/features/account/pages/MyBookingsPage')).MyBookingsPage }),
      },
      {
        path: 'bookings/:bookingNo',
        lazy: async () => ({ Component: (await import('@/features/account/pages/MyBookingPage')).MyBookingPage }),
      },
      { path: 'profile', lazy: async () => ({ Component: (await import('@/features/account/pages/ProfilePage')).ProfilePage }) },
      // The quote email links to trips/:tripNo (backend: SendCustomTripQuotedMessages).
      { path: 'trips', lazy: async () => ({ Component: (await import('@/features/trips/pages/MyTripsPage')).MyTripsPage }) },
      { path: 'trips/:tripNo', lazy: async () => ({ Component: (await import('@/features/trips/pages/MyTripPage')).MyTripPage }) },
      // "Accept & pay" (Day 15): names of the travellers, then the usual payment page.
      {
        path: 'trips/:tripNo/accept',
        lazy: async () => ({ Component: (await import('@/features/trips/pages/AcceptQuotePage')).AcceptQuotePage }),
      },
    ],
  },
  {
    // Staff only - the same roles as the API's AdminArea policy.
    path: 'admin',
    element: (
      <RequireRole roles={staffRoles}>
        <AdminLayout />
      </RequireRole>
    ),
    errorElement: <RouteErrorPage />,
    children: [
      {
        // Super Admin only (dashboardRoles). AdminLayout sends everyone else on
        // to the first page of their menu - so /admin is every staff member's "home".
        index: true,
        lazy: async () => ({ Component: (await import('@/features/admin/pages/AdminDashboardPage')).AdminDashboardPage }),
      },
      {
        // Bookings, payments, refunds: Super Admin, Manager, Accounts - the API's ViewBookings policy.
        element: (
          <RequireRole roles={bookingDeskRoles} redirectTo="/admin">
            <Outlet />
          </RequireRole>
        ),
        children: [
          {
            path: 'bookings',
            lazy: async () => ({ Component: (await import('@/features/admin/operations/pages/AdminBookingsPage')).AdminBookingsPage }),
          },
          {
            path: 'bookings/:bookingNo',
            lazy: async () => ({ Component: (await import('@/features/admin/operations/pages/AdminBookingPage')).AdminBookingPage }),
          },
          {
            path: 'payments',
            lazy: async () => ({ Component: (await import('@/features/admin/operations/pages/AdminPaymentsPage')).AdminPaymentsPage }),
          },
          {
            path: 'refunds',
            lazy: async () => ({ Component: (await import('@/features/admin/operations/pages/AdminRefundsPage')).AdminRefundsPage }),
          },
        ],
      },
      // Custom trips: every staff member (only Sales, Manager and Super Admin may quote - the page checks).
      {
        path: 'custom-trips',
        lazy: async () => ({ Component: (await import('@/features/admin/customTrips/pages/AdminCustomTripsPage')).AdminCustomTripsPage }),
      },
      {
        // The staff alert email links here (backend: SendCustomTripSubmittedEmails).
        path: 'custom-trips/:tripNo',
        lazy: async () => ({ Component: (await import('@/features/admin/customTrips/pages/AdminCustomTripPage')).AdminCustomTripPage }),
      },
      {
        // The catalogue: Super Admin, Manager, Sales - the API's ManageCatalogue policy.
        element: (
          <RequireRole roles={catalogueRoles} redirectTo="/admin">
            <Outlet />
          </RequireRole>
        ),
        children: [
          {
            path: 'destinations',
            lazy: async () => ({
              Component: (await import('@/features/admin/destinations/pages/AdminDestinationsPage')).AdminDestinationsPage,
            }),
          },
          {
            path: 'packages',
            lazy: async () => ({ Component: (await import('@/features/admin/packages/pages/AdminPackagesPage')).AdminPackagesPage }),
          },
          {
            // "new" is listed before ":packageId", but React Router ranks a fixed
            // segment above a parameter anyway - /admin/packages/new never reaches the edit route.
            path: 'packages/new',
            lazy: async () => ({
              Component: (await import('@/features/admin/packages/pages/AdminPackageEditPage')).AdminPackageEditPage,
            }),
          },
          {
            path: 'packages/:packageId',
            lazy: async () => ({
              Component: (await import('@/features/admin/packages/pages/AdminPackageEditPage')).AdminPackageEditPage,
            }),
          },
          {
            path: 'categories',
            lazy: async () => ({
              Component: (await import('@/features/admin/categories/pages/AdminCategoriesPage')).AdminCategoriesPage,
            }),
          },
        ],
      },
      {
        // Super Admin only - the same as the API's ManageStaff policy. Other staff go back to their admin home.
        path: 'staff',
        element: (
          <RequireRole roles={staffManagerRoles} redirectTo="/admin">
            <Outlet />
          </RequireRole>
        ),
        children: [{ index: true, lazy: async () => ({ Component: (await import('@/features/admin/staff/pages/AdminStaffPage')).AdminStaffPage }) }],
      },
      {
        path: 'system',
        element: (
          <RequireRole roles={systemHealthRoles} redirectTo="/admin">
            <Outlet />
          </RequireRole>
        ),
        children: [{ index: true, lazy: async () => ({ Component: (await import('@/features/health/HealthStatus')).HealthStatus }) }],
      },
    ],
  },
])
