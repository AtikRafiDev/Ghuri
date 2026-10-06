# Ghuri – Before go-live (Day 17)

Everything collected during Days 10–16 that must be done or decided before the
site goes public. Work top to bottom; most items are settings, not code.

## Content from the client

- [ ] `frontend/src/shared/config/site.ts`: real **phone, email, address** and **WhatsApp number** (all placeholders now).
- [ ] `appsettings.json` → `Agency`: real name, address, phone, email, website (printed on every invoice and voucher).
- [ ] **About** page: the agency's story, team, trade licence and tourism registration numbers.
- [ ] **Terms, Privacy, Refund policy**: reviewed by the client and a lawyer, then remove `draft` from each page (`features/pages/pages/*.tsx`). SSLCommerz asks for these before approving a live account.
- [ ] Refund promise "within 7 working days" (Refund policy page): confirm the agency can keep it.
- [ ] Real packages, photos and departures loaded.
- [ ] QuestPDF **Community licence**: confirm the agency's yearly revenue is under US$1M (otherwise a paid licence).

## Secrets and settings (production server)

- [ ] **JWT signing key**: a NEW key, only as the environment variable `Jwt__SigningKey`. The development key is in Git (`appsettings.Development.json`) - never use it anywhere else; moving it back to user-secrets is recommended.
- [ ] `Site__PublicUrl` = the real address, e.g. `https://www.ghuri.com` (links in emails, the sitemap). The API refuses to start without it.
- [ ] `Agency__BookingsEmail` = the shared inbox for new custom-trip requests.
- [ ] `Auth__PasswordResetUrl` = `https://www.ghuri.com/reset-password`.
- [ ] Email: `Email__Sender=Smtp` and the real mail server (`Email__Smtp__Host`, `Port`, `Security`, `UserName`, `Password`), `Email__FromAddress`.
- [ ] SSLCommerz **live**: `PaymentGateway__SslCommerz__UseSandbox=false`, live `StoreId` / `StorePassword`, `CallbackBaseUrl` = the real address, `IpnUrl` = `https://www.ghuri.com/api/v1/payments/sslcommerz/ipn` (also set the IPN URL in the SSLCommerz merchant panel).
- [ ] Database: run the migrations (`.\ef.cmd database update` against production, or a migration bundle), then `-- seed` once (Super Admin + refund policy).

## Server (Nginx in front of the API)

- [ ] HTTPS certificate; redirect http → https.
- [ ] Nginx forwards `/api`, `/health`, `/files`, **`/sitemap.xml`** and **`/robots.txt`** to the API; everything else serves the React build (`npm run build` → `dist/`), with unknown paths falling back to `index.html`.
- [ ] Nginx sends `X-Forwarded-For` and `X-Forwarded-Proto`. The API trusts these only from the same machine; if Nginx runs in a **separate container**, add its network to `KnownNetworks` (Program.cs, `UseForwardedHeaders`) or every visitor shares one rate limit.
- [ ] Security headers for the website's own files (the API already sends its own): `Content-Security-Policy`, `X-Content-Type-Options: nosniff`, `Referrer-Policy`.
  - `script-src 'self'` is enough: the theme picker is a file (`/theme-init.js`), not inline code.
  - `style-src` needs `'self' 'unsafe-inline'`: `index.html` inlines the loading-screen styles, and React sets `style=""` attributes (chart sizes, animation delays).
  - `img-src 'self' data:`: uploaded photos, plus the contour-line background texture, which is a `data:` SVG in `index.css`.
- [ ] Linux: install a Bengali font (`fonts-noto-core` / Noto Sans Bengali) so names typed in Bangla print on vouchers.
- [ ] Docker for smtp4dev is development-only - don't run it on the server.

## Smoke test on the live site

- [ ] `/health` answers. Uptime monitoring points at it.
- [ ] Register, book the cheapest package, pay **one real small payment**, receive the voucher email; refund it from the admin panel.
- [ ] Submit and quote one custom trip.
- [ ] Database backups scheduled and one restore tried.
