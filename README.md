# Ghuri — Tour & Travel Booking Platform

A tour & travel booking system built as a learning project, following a
Clean Architecture + CQRS blueprint.

- **Backend:** ASP.NET Core on .NET 10, Clean Architecture (Domain →
  Application → Infrastructure → Api), CQRS, EF Core (code-first).
- **Database:** SQL Server (LocalDB for local development).
- **Frontend:** React + TypeScript (not started yet).

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
| [Node.js](https://nodejs.org/) (LTS) | Frontend (once it exists) | `node --version` |

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

---

## 5. Run the API

```
cd backend
dotnet run --project src/Ghuri.Api
```

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
