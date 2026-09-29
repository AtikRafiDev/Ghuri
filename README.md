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
- The account has **no password**. Set one with **Forgot password**; in
  development the reset link is written to the API's console. So no
  password ever sits in a config file or in git. Until the frontend's login
  page exists, do it with `backend/src/Ghuri.Api/Ghuri.Api.http` (steps
  1–4 in that file: request link → copy token from the console → set
  password → log in).
- Safe to run again: if a Super Admin already exists, it does nothing.
- The 5 roles (SuperAdmin, Manager, Sales, Accounts, Customer) are not
  created here - they come with the migrations in `database update` above.
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

In development, "Forgot password" emails are written to the **API's
console** - copy the link from there into the browser.

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
