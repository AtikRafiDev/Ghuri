import { createBrowserRouter, Outlet } from 'react-router'
import { staffRoles } from '@/features/auth/auth.types'
import { RequireAuth } from '@/features/auth/components/RequireAuth'
import { RequireRole } from '@/features/auth/components/RequireRole'
import { AccountLayout } from './layouts/AccountLayout'
import { AdminLayout } from './layouts/AdminLayout'
import { PublicLayout } from './layouts/PublicLayout'
import { NotFoundPage } from './NotFoundPage'
import { RouteErrorPage } from './RouteErrorPage'

// Every page is LAZY: its code is downloaded only when first visited
// (blueprint 13.2: "route-level code splitting - admin bundles never load
// on the public site"). A customer's phone never downloads admin screens.
export const router = createBrowserRouter([
  {
    element: <PublicLayout />,
    errorElement: <RouteErrorPage />,
    children: [
      { index: true, lazy: async () => ({ Component: (await import('@/features/home/pages/HomePage')).HomePage }) },
      {
        path: 'packages',
        lazy: async () => ({ Component: (await import('@/features/catalog/pages/PackagesPage')).PackagesPage }),
      },
      {
        path: 'packages/:slug',
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
      { path: 'login', lazy: async () => ({ Component: (await import('@/features/auth/pages/LoginPage')).LoginPage }) },
      { path: 'register', lazy: async () => ({ Component: (await import('@/features/auth/pages/RegisterPage')).RegisterPage }) },
      {
        path: 'forgot-password',
        lazy: async () => ({ Component: (await import('@/features/auth/pages/ForgotPasswordPage')).ForgotPasswordPage }),
      },
      {
        // The address the reset EMAIL links to (API setting Auth:PasswordResetUrl).
        path: 'reset-password',
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
        index: true,
        lazy: async () => ({ Component: (await import('@/features/admin/pages/AdminDashboardPage')).AdminDashboardPage }),
      },
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
      { path: 'system', lazy: async () => ({ Component: (await import('@/features/health/HealthStatus')).HealthStatus }) },
    ],
  },
])
