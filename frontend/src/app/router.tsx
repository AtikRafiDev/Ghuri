import { createBrowserRouter } from 'react-router'
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
        path: 'categories',
        lazy: async () => ({
          Component: (await import('@/features/admin/categories/pages/AdminCategoriesPage')).AdminCategoriesPage,
        }),
      },
      { path: 'system', lazy: async () => ({ Component: (await import('@/features/health/HealthStatus')).HealthStatus }) },
    ],
  },
])
