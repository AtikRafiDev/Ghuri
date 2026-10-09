# Ghuri — Tour & Travel Booking Platform

A tour & travel booking system built as a learning project, following a
Clean Architecture + CQRS blueprint.

- **Backend:** ASP.NET Core on .NET 10, Clean Architecture (Domain →
  Application → Infrastructure → Api), CQRS, EF Core (code-first).
- **Database:** SQL Server (LocalDB for local development).
- **Frontend:** React 19 + TypeScript (Vite), Tailwind CSS + shadcn/ui,
  React Router, TanStack Query, React Hook Form + Zod.

This file exists so the whole project can be set up on a brand-new PC by
following the commands below, in order — nothing here should require
guessing or remembering.

---

## 1. Prerequisites (install these first)

| Tool | Used for | Check it's installed |
|---|---|---|
| [.NET 10 SDK](https://dotnet.microsoft.com/download) | Building/running the backend | `dotnet --list-sdks` |
| [Git](https://git-scm.com/) | Version control | `git --version` |
| SQL Server LocalDB | Local database (ships with Visual Studio's "Data storage and processing" workload, or the standalone [SQL Server Express LocalDB installer](https://learn.microsoft.com/sql/database-engine/configure-windows/sql-server-express-localdb)) | `sqllocaldb info` |
| [Node.js](https://nodejs.org/) 24 LTS (same as CI) — or `winget install OpenJS.NodeJS.LTS` | Frontend | `node --version` — **fully restart VS Code** after installing (File → Exit): its terminals keep the PATH from when VS Code started. If PowerShell then says *"npm.ps1 cannot be loaded because running scripts is disabled"*, run `Set-ExecutionPolicy -Scope CurrentUser RemoteSigned` once, or type `npm.cmd` instead of `npm`. |

---

## 2. Clone the repository

```
git clone <repository-url> Ghuri
cd Ghuri
```

### Set your git identity for this repo only (not your global identity)

```
git config user.name "Your Name"
git config user.email "you@example.com"
```

No `--global` flag — this way it only applies inside this repo's folder and
never overrides your identity in other projects on the same machine.

---

## 3. Restore and build the backend

```
cd backend
dotnet tool restore
dotnet restore
dotnet build
```

- `dotnet tool restore` reads `backend/.config/dotnet-tools.json` and
  installs the exact same local CLI tools (like `dotnet-ef`) that this
  project uses — nothing to install manually, no version mismatches.
- Expect **0 Warning(s), 0 Error(s)** at the end of `dotnet build`.

---

## 4. Create the local database

The database is **code-first**: nothing is created until you run EF Core
migrations against your machine's SQL Server LocalDB instance.

```
cd backend
.\ef.cmd database update
```

This reads the connection string from
`backend/src/Ghuri.Api/appsettings.Development.json`:

```
Server=(localdb)\MSSQLLocalDB;Database=GhuriDb;Trusted_Connection=True;TrustServerCertificate=True;
```

- Uses your Windows login (`Trusted_Connection=True`) — no password, so
  this connection string is safe to commit to git.
- Creates a database named **GhuriDb** on your default LocalDB instance
  the first time you run this command. If `GhuriDb` doesn't exist yet,
  EF Core creates it automatically — you never create it by hand.

### Check the database was created

```
sqllocaldb info MSSQLLocalDB
```
Should show `State: Running`.

### Create the first Super Admin

```
cd backend
dotnet run --project src/Ghuri.Api -- seed
```

- Creates the Super Admin from the `Seed:SuperAdmin` settings in
  `appsettings.Development.json` (name, email, phone), then exits - it
  does not start the web server.
- The account has **no password**. Set one with **Forgot password** on the
  login page; in development the reset email lands in smtp4dev (section 8).
  So no password ever sits in a config file or in git.
- Also creates the global **cancellation policy** (refund % by days before
  the trip: 30+ days 100%, 15+ 50%, 7+ 25%, less 0%) if there is none.
- Safe to run again: if a Super Admin or a global policy already exists,
  it's left as it is.
- The 5 roles (SuperAdmin, Manager, Sales, Accounts, Customer) are not
  created here - they come with the migrations in `database update` above.
- Staff accounts (Manager, Sales, Accounts) are made by the Super Admin in
  the admin panel: **Admin → Staff → Add staff member**. The new person is
  emailed a link to set their own password. Customers sign up themselves.
- Other environments: set `Seed__SuperAdmin__FullName`,
  `Seed__SuperAdmin__Email` and `Seed__SuperAdmin__Phone` as environment
  variables, run `database update`, then run `seed` once.

---

## 5. Run the API

### One-time: create your JWT signing key

The API signs login tokens with a secret key, and **refuses to start
without one**. The key is a secret, so it's not in git - each machine
creates its own, stored by `dotnet user-secrets` in your Windows profile
(`%APPDATA%\Microsoft\UserSecrets\`), outside the repository.

In PowerShell:
```
cd backend
$bytes = New-Object byte[] 64; [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
dotnet user-secrets set "Jwt:SigningKey" ([Convert]::ToBase64String($bytes)) --project src/Ghuri.Api
```

- Check it's there: `dotnet user-secrets list --project src/Ghuri.Api`
- On a server, set the environment variable `Jwt__SigningKey` instead.
- Changing the key logs everyone out (their tokens no longer verify) -
  harmless locally.
- Run only `set` on a new PC, **not** `dotnet user-secrets init`: `init`
  was done once and added the `UserSecretsId` line to
  `src/Ghuri.Api/Ghuri.Api.csproj`, which every clone already has.

#### Does every machine need the same key?

No - each PC creates its **own, different** key, and that's intended.
The key only has to match itself: the same API on the same PC both
creates a token and later checks it. A token made on one PC won't work
on another, but nobody needs that - each PC also has its own database
and its own users.

When a key **does** have to be shared:

| Situation | Same key needed? | How it gets there |
|---|---|---|
| Two developer PCs | No | Each runs `set` itself |
| Staging server vs production server | **No - and they should differ** | A staging token must never work on the live site |
| Several copies of the API behind one address (production at scale) | **Yes** | A token signed by copy 1 must be accepted by copy 2 |

- **Servers:** create the key once and give it to the server as the
  environment variable `Jwt__SigningKey`. It never goes through git or
  chat. Where that variable lives depends on the hosting - e.g. a `.env`
  file on the server that isn't in git, or GitHub Actions secrets
  injected during deployment (Day 7 staging, Day 14 production).
- **Two people truly needing the same key:** share it through a password
  manager - never through git, email or chat.
- **Not related to the signing key:** the Super Admin's password. That's a
  hash stored in each PC's own database - each PC runs `seed` (section 4)
  and sets its own password.

### Start it

```
cd backend
dotnet run --project src/Ghuri.Api
```

---

## 6. Run the frontend

In a **second** terminal, with the API from step 5 still running:

```
cd frontend
npm ci
npm run dev
```

Open **http://localhost:5173**.

| Page | Who |
|---|---|
| `/` · `/login` · `/register` · `/forgot-password` | everyone |
| `/account` | any logged-in user |
| `/admin` · `/admin/system` (API health) | staff only (SuperAdmin, Manager, Sales, Accounts) |

In development, every email (password reset, booking confirmation with
its PDFs) goes to **smtp4dev** - see section 8.

- `npm ci` installs exactly the versions in `package-lock.json` (first time,
  or after pulling changes to it).
- The page calls the API through Vite's dev proxy (`frontend/vite.config.ts`),
  which forwards `/api` and `/health` to `http://localhost:5176` - so the
  browser sees one address and no CORS setup is needed.
- If the page shows "API unreachable", the API from step 5 isn't running.

| Command (in `frontend/`) | What it does |
|---|---|
| `npm run dev` | Development server with instant reload on save |
| `npm run lint` | ESLint - code-quality checks |
| `npm run build` | Type-check (`tsc`) + production build into `dist/` |

---

## 7. Public address for SSLCommerz (tunnel)

`localhost` exists only on your PC, so SSLCommerz's server can't reach it.
Its **IPN** ("this payment is done" message, server to server) would never
arrive. A **Cloudflare quick tunnel** gives your running app a temporary
public `https://….trycloudflare.com` address that forwards to Vite on this
PC. Vite already forwards `/api` to the API, so **one tunnel covers the
website, the API and SSLCommerz's messages**. You don't need a Cloudflare
account or a domain. It's a development tool only: there's no uptime
guarantee, and the address changes every time you start it.

```
SSLCommerz / any phone ──https──► xxxx.trycloudflare.com ──► cloudflared (this PC)
                                                              └─► Vite :5173 ──/api──► API :5176
```

### One-time: install cloudflared

```
winget install --id Cloudflare.cloudflared
```

Close and reopen the terminal, then check: `cloudflared --version`.

### Every time

1. Start the API (section 5) and the frontend (section 6) as usual.
2. In a **third** terminal:
   ```
   cloudflared tunnel --url http://localhost:5173
   ```
   After a few seconds it prints a box with
   `https://<random-words>.trycloudflare.com`. That's your public address.
   Leave this terminal open: closing it ends the tunnel.
3. Tell the API that address (replace the example with yours):
   ```
   cd backend
   dotnet user-secrets set "PaymentGateway:SslCommerz:CallbackBaseUrl" "https://random-words.trycloudflare.com" --project src/Ghuri.Api
   dotnet user-secrets set "PaymentGateway:SslCommerz:IpnUrl" "https://random-words.trycloudflare.com/api/v1/payments/sslcommerz/ipn" --project src/Ghuri.Api
   ```
4. **Restart the API** (Ctrl+C, then `dotnet run` again). Settings are read at start.
5. Open the **tunnel address** in the browser, not localhost. Log in there:
   the login cookie belongs to the address you logged in on.

- The address works from any device, so you can test bKash/Nagad on your phone.
- **New tunnel = new address:** repeat steps 3–4. A stale address sends
  customers back to a dead page after paying.
- **Back to plain localhost:**
  ```
  dotnet user-secrets remove "PaymentGateway:SslCommerz:CallbackBaseUrl" --project src/Ghuri.Api
  dotnet user-secrets remove "PaymentGateway:SslCommerz:IpnUrl" --project src/Ghuri.Api
  ```
- Through the tunnel, the API sees every visitor as Vite (127.0.0.1), so
  login rate limits are shared by everyone using it. That's fine for one
  developer; Nginx passes the real visitor IP in production.

---

## 8. See the emails (smtp4dev)

In development the API sends real emails over SMTP - to **smtp4dev**, a
fake mail server that keeps every email in a web inbox instead of
delivering it. Nothing reaches a real mailbox. It runs in **Docker**
(`docker-compose.yml` in the repository root), so Docker Desktop must be
running.

From the repository root (`F:\Ghuri`):
```
docker compose up -d
```
Open **http://localhost:5000**: every email the API sends shows up there,
attachments (the e-voucher and invoice PDFs) included.

| Command (in the repository root) | What it does |
|---|---|
| `docker compose up -d` | Start smtp4dev in the background (the first time it downloads the image) |
| `docker compose ps` | Is it running? |
| `docker compose logs -f smtp4dev` | Its log, live (Ctrl+C to stop watching) |
| `docker compose down` | Stop it (the emails are kept) |
| `docker compose down -v` | Stop it and delete the kept emails |

- `restart: unless-stopped`: once started, it comes back by itself whenever
  Docker Desktop starts - until you run `docker compose down`.
- The API sends to `localhost:25` (`Email:Smtp` in
  `appsettings.Development.json`); Docker passes it on to the container.
- **"port is already allocated"**: something else uses port 25 or 5000 -
  e.g. smtp4dev started as a dotnet tool earlier. Close that, then
  `docker compose up -d` again.
- **smtp4dev not running?** A password reset fails with an error, and the
  booking confirmation email waits: the outbox job retries it, waiting
  longer each time (10 s, 20 s, 40 s… 10 tries over ~3 hours). Start
  smtp4dev and it goes out on the next try.
- No smtp4dev at all? Set `"Sender": "Log"` under `Email` in
  `appsettings.Development.json`: emails are then written to the API's
  console (without the PDFs).

---

## 9. Share with the QA team (ngrok)

Testers on **other PCs** open the website through **ngrok**. The API, the
database and smtp4dev keep running on this PC. Only the website gets a
public address, and the website already forwards `/api` to the API, the
same way as in section 7.

```
Tester's PC ──https──► your-name.ngrok-free.dev ──► ngrok (this PC)
SSLCommerz ───────────┘                              └─► built website :4173 ──/api──► API :5176
```

ngrok's free plan gives your account **one fixed address** (shown as
`your-name.ngrok-free.dev` here; yours may end in `.ngrok-free.app`).
Unlike the Cloudflare tunnel in section 7, it stays the same every time,
so the one-time settings below are made once. Use one tunnel or the
other: both use the same `CallbackBaseUrl` / `IpnUrl` settings.

### Why testers get the built website, not `npm run dev`

- **The free plan's monthly limit is shared by the whole team:** 20,000
  requests and 1 GB. `npm run dev` sends each of the ~210 source files
  separately, so the first page load alone is 200+ requests. The built
  website (`npm run build`) loads about 11 files.
- **Testers get a fixed version.** You keep coding on `npm run dev` at
  `localhost:5173`. Testers see your changes only when you build again.
  (The API and the database are shared: restarting the API interrupts
  testers for a few seconds, and everyone's bookings are in one database.)

### One-time

1. **Find your address:** run `ngrok http 4173` once. The `Forwarding`
   line shows `https://<your-name>.ngrok-free.dev -> http://localhost:4173`.
   Press Ctrl+C.
2. **Tell the API that address** (replace the example with yours, in all four):
   ```
   cd backend
   dotnet user-secrets set "Site:PublicUrl" "https://your-name.ngrok-free.dev" --project src/Ghuri.Api
   dotnet user-secrets set "Auth:PasswordResetUrl" "https://your-name.ngrok-free.dev/reset-password" --project src/Ghuri.Api
   dotnet user-secrets set "PaymentGateway:SslCommerz:CallbackBaseUrl" "https://your-name.ngrok-free.dev" --project src/Ghuri.Api
   dotnet user-secrets set "PaymentGateway:SslCommerz:IpnUrl" "https://your-name.ngrok-free.dev/api/v1/payments/sslcommerz/ipn" --project src/Ghuri.Api
   ```

   | Setting | What uses it |
   |---|---|
   | `Site:PublicUrl` | Links in custom-trip emails (the quote, "See your request", the staff's admin-panel link) |
   | `Auth:PasswordResetUrl` | The link in "reset your password" and staff-invite emails |
   | `SslCommerz:CallbackBaseUrl` | Where SSLCommerz sends the tester's browser after paying |
   | `SslCommerz:IpnUrl` | Where SSLCommerz's server says "payment done" |

   Testers can't open `localhost` links: on their PC, `localhost` is
   their own machine.
3. **Your own JWT signing key.** The repository is public, so the key in
   `appsettings.Development.json` can be read by anyone. Once the API is
   reachable from the internet, someone could use that key to sign their
   own Super Admin login. Run the key step in section 5 ("One-time:
   create your JWT signing key"). User-secrets take priority over
   `appsettings.Development.json`. Everyone has to log in again once.
4. **Restart the API.** Settings are read only at start.

Check: `dotnet user-secrets list --project src/Ghuri.Api` shows five settings.

### Every time

1. Start the API (section 5) and smtp4dev (section 8) as usual.
2. Build the website and serve the build:
   ```
   cd frontend
   npm run build
   npm run preview
   ```
   It shows `Local: http://localhost:4173/`. Leave it open. (If it says
   4174, something else is using 4173 and ngrok would point at the wrong
   port. Close that, then try again.)
3. In another terminal:
   ```
   ngrok http 4173
   ```
   Leave it open. Ctrl+C here stops the sharing.
4. Give the testers the `https://…ngrok-free.dev` address.

**A new version for the testers:** Ctrl+C the preview, then `npm run build`
and `npm run preview` again. The address stays the same. A backend change:
restart the API.

### Before the first tester: one test payment

Open the **ngrok address** yourself (not localhost), book something and
pay in the SSLCommerz sandbox. After paying, SSLCommerz sends the browser
back to us with a form submission from **its own** site, and ngrok's
warning page (below) might get in the way. You should land on "Payment
received". If you see ngrok's warning page instead, the payment is still
confirmed: SSLCommerz's server-to-server message never gets the warning
page, so **My bookings** shows Confirmed. But testers would see the
warning page after every payment, and that needs a fix before they start.

### What testers will notice (not bugs)

- **ngrok's warning page** ("You are about to visit…") on the first visit:
  click **Visit Site**. The browser then skips it for 7 days. Paid ngrok
  plans don't show it.
- **Emails** land in smtp4dev on **this** PC (`http://localhost:5000`),
  which testers can't open. Show them on your screen.
- **"Too many attempts"** on login, register or forgot-password: those
  allow 5 tries a minute per internet address. Testers in one office share
  one address, so they share those 5. Wait a minute and try again.

### Limits of the free plan

- **20,000 requests and 1 GB a month** for everyone together. The demo
  photos alone are about 100 MB. Each tester's first look through the
  catalogue downloads them, and a private/incognito window downloads them
  again. ngrok's dashboard (dashboard.ngrok.com) shows how much is used.
- If the team runs out: the **Hobbyist** plan (US$10 a month) has 100,000
  requests and 5 GB, and no warning page. Or switch to section 7's
  Cloudflare tunnel (no monthly limit, but a new address each time and
  the four settings again).

### Back to plain localhost

The four address settings stay in place, so next time skip straight to
"Every time". While they're set, emails and payments started on
`localhost:5173` also link to and return to the ngrok address. To undo:
```
cd backend
dotnet user-secrets remove "Site:PublicUrl" --project src/Ghuri.Api
dotnet user-secrets remove "Auth:PasswordResetUrl" --project src/Ghuri.Api
dotnet user-secrets remove "PaymentGateway:SslCommerz:CallbackBaseUrl" --project src/Ghuri.Api
dotnet user-secrets remove "PaymentGateway:SslCommerz:IpnUrl" --project src/Ghuri.Api
```
Keep the JWT key: it's better than the one in Git anyway.

---

## Everyday commands (reference)

### Git

| Command | What it does |
|---|---|
| `git status` | See what's changed |
| `git add .` | Stage all changes |
| `git commit -m "subject" -m "body"` | Commit staged changes (multiple `-m` = multiple paragraphs; works in cmd.exe/PowerShell without needing an editor) |
| `git log --oneline` | See commit history, one line each |
| `git log -1` | See full details of the last commit |
| `git commit --amend -m "..." -m "..."` | Replace the last commit's message entirely (only safe before it's pushed/shared) |

### Branches

| Branch | Purpose |
|---|---|
| `main` | Always working, always releasable. Only receives finished, tested work from `develop`. |
| `develop` | Day-to-day work happens here. |

```
git switch develop                 # start working
git switch main                    # when develop is ready to release:
git merge develop
git push origin main develop
```

### Continuous Integration

`.github/workflows/ci.yml` runs on every push and pull request: a fresh
GitHub server restores, builds (Release, **warnings treated as errors**),
and runs every test. Check the result on the repository's **Actions** tab,
or the ✓ / ✗ next to each commit. To run the same checks locally first:

```
cd backend
dotnet build --configuration Release -p:ContinuousIntegrationBuild=true
dotnet test --configuration Release --no-build
```

### EF Core migrations

Every `dotnet ef` command needs `--project src/Ghuri.Infrastructure`
(where `AppDbContext` lives) and `--startup-project src/Ghuri.Api` (the
runnable project with the connection string). Typing both every time is
tedious, so `backend/ef.cmd` wraps `dotnet ef` with those two flags baked
in — run it from the `backend` folder exactly like `dotnet ef`, just
shorter:

```
.\ef.cmd migrations add <Name>
.\ef.cmd database update
.\ef.cmd migrations list
```

(In **PowerShell** the `.\` prefix is required — PowerShell refuses to run
a script from the current folder unqualified, on purpose, as a security
measure. In classic **cmd.exe**, plain `ef migrations add <Name>` also
works.)

| Command | What it does |
|---|---|
| `.\ef.cmd migrations add <Name>` | Generate a new migration after changing an entity or its EF configuration |
| `.\ef.cmd migrations remove` | Delete the most recent **unapplied** migration (safe only if `database update` hasn't been run for it yet) |
| `.\ef.cmd database update` | Apply all pending migrations to the real database — this is what actually changes the schema |
| `.\ef.cmd migrations list` | See every migration and whether it's applied |
| `dotnet ef --version` | Confirm the tool is installed and its version (no project needed for this one) |

Long form, in case `ef.cmd` isn't available for some reason (e.g. a shell that can't run `.cmd` files):
```
dotnet ef migrations add <Name> --project src/Ghuri.Infrastructure --startup-project src/Ghuri.Api
dotnet ef database update --project src/Ghuri.Infrastructure --startup-project src/Ghuri.Api
```

### Build / test

| Command | What it does |
|---|---|
| `dotnet build` | Build every project in the solution |
| `dotnet test` | Run every test project |

The database tests (`Ghuri.Api.IntegrationTests/Database`) need only
SQL Server LocalDB, no Docker. Each test class creates its own
`GhuriTest_<random>` database, applies the migrations, and deletes it at
the end. Your `GhuriDb` is never touched. If a run is killed halfway, a
leftover `GhuriTest_...` database is harmless: delete it in SSMS if you
like.

---

## Project structure

```
Ghuri/
├── .gitignore, .editorconfig, .gitattributes   ← repo-wide conventions
├── README.md                                    ← this file
└── backend/
    ├── Ghuri.slnx                               ← solution file
    ├── Directory.Build.props                    ← shared settings for every project (target framework, nullable, etc.)
    ├── Directory.Packages.props                 ← every NuGet package's version, pinned in one place
    ├── nuget.config                             ← restricts package restore to nuget.org only
    ├── .config/dotnet-tools.json                ← local CLI tools (dotnet-ef), versioned with the repo
    ├── ef.cmd                                    ← shortcut: ".\ef.cmd migrations add X" instead of the full dotnet ef command
    ├── src/
    │   ├── Ghuri.Domain/                        ← entities, business rules. No dependencies on anything.
    │   ├── Ghuri.Application/                   ← commands, queries, handlers. Depends only on Domain.
    │   ├── Ghuri.Infrastructure/                ← EF Core, AppDbContext, external services. Depends on Application.
    │   └── Ghuri.Api/                           ← controllers, Program.cs. Depends on everything.
    └── tests/
        ├── Ghuri.Domain.Tests/
        ├── Ghuri.Application.Tests/
        ├── Ghuri.Api.IntegrationTests/
        └── Ghuri.ArchitectureTests/             ← enforces the dependency rules above automatically
```

---

## Appendix — how this solution was originally scaffolded

Kept here for reference, in case a new project/layer needs to be added the
same way later. **You do not need to run these again** after cloning —
they're already done and committed.

```
cd backend

# Solution + 4 layers
dotnet new sln -n Ghuri --format slnx
dotnet new classlib -n Ghuri.Domain         -o src/Ghuri.Domain
dotnet new classlib -n Ghuri.Application    -o src/Ghuri.Application
dotnet new classlib -n Ghuri.Infrastructure -o src/Ghuri.Infrastructure
dotnet new webapi   -n Ghuri.Api            -o src/Ghuri.Api --use-controllers

# xUnit v3 test projects (the built-in "xunit" template is v2, so this
# template pack was installed once, machine-wide, first)
dotnet new install xunit.v3.templates
dotnet new xunit3 -n Ghuri.Domain.Tests         -o tests/Ghuri.Domain.Tests
dotnet new xunit3 -n Ghuri.Application.Tests    -o tests/Ghuri.Application.Tests
dotnet new xunit3 -n Ghuri.Api.IntegrationTests -o tests/Ghuri.Api.IntegrationTests
dotnet new xunit3 -n Ghuri.ArchitectureTests    -o tests/Ghuri.ArchitectureTests

# Add every project to the solution
dotnet sln add (Get-ChildItem -Recurse -Filter *.csproj src).FullName   --solution-folder src
dotnet sln add (Get-ChildItem -Recurse -Filter *.csproj tests).FullName --solution-folder tests

# Wire the Clean Architecture dependency direction (inward only)
dotnet add src/Ghuri.Application    reference src/Ghuri.Domain
dotnet add src/Ghuri.Infrastructure reference src/Ghuri.Application
dotnet add src/Ghuri.Api            reference src/Ghuri.Infrastructure src/Ghuri.Application

dotnet add tests/Ghuri.Domain.Tests         reference src/Ghuri.Domain
dotnet add tests/Ghuri.Application.Tests    reference src/Ghuri.Application
dotnet add tests/Ghuri.Api.IntegrationTests reference src/Ghuri.Api
dotnet add tests/Ghuri.ArchitectureTests    reference src/Ghuri.Api

# Local EF Core CLI tool (versioned with the repo via dotnet-tools.json)
dotnet new tool-manifest
dotnet tool install dotnet-ef --version 10.0.12

# First migration + first database creation
dotnet ef migrations add InitialCreate --project src/Ghuri.Infrastructure --startup-project src/Ghuri.Api
dotnet ef database update              --project src/Ghuri.Infrastructure --startup-project src/Ghuri.Api
```

Repo hygiene files, generated once at the repository root:
```
dotnet new gitignore
dotnet new editorconfig
```
