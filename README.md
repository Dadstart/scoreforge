# ScoreForge

Web-first game scorekeeping for Cribbage, American Canasta, and more.

## Stack

- **API:** ASP.NET Core (.NET 11 preview 6), cookie OAuth (Google / Microsoft), SignalR, EF Core
- **Client:** React + Redux Toolkit + Tailwind (Vite)
- **Database:** PostgreSQL via **Podman** Compose (Npgsql). Unit tests use SQLite.

SDK is pinned in [`global.json`](global.json) to `11.0.100-preview.6.26359.118`.

## Quick start

Requires [Podman](https://podman.io/) / Podman Desktop (Docker Desktop is not required). Put `podman.exe` on PATH (often `%LOCALAPPDATA%\Programs\Podman`).

```powershell
# Start Postgres (starts the Podman machine if needed)
./scripts/db.ps1 -Action Start

# Or start DB + API + Vite together:
./scripts/dev.ps1 -Action Start
```

Open http://127.0.0.1:5173. When OAuth secrets are empty, Development exposes a **Developer** sign-in provider.

Equivalent manual compose:

```powershell
podman machine start   # if the machine is stopped
podman compose up -d
```

### .NET SDK

If `dotnet --version` is not preview 6 after cloning:

```powershell
irm https://dot.net/v1/dotnet-install.ps1 | iex
# or:
& "$env:TEMP\dotnet-install.ps1" -Version 11.0.100-preview.6.26359.118
```

Ensure that SDK is on `PATH` (the install script defaults to `%LOCALAPPDATA%\dotnet`).

### OAuth secrets

```powershell
dotnet user-secrets set "Authentication:Google:ClientId" "..." --project src/ScoreForge.Api
dotnet user-secrets set "Authentication:Google:ClientSecret" "..." --project src/ScoreForge.Api
dotnet user-secrets set "Authentication:Microsoft:ClientId" "..." --project src/ScoreForge.Api
dotnet user-secrets set "Authentication:Microsoft:ClientSecret" "..." --project src/ScoreForge.Api
```

### Database

Default connection (matches `docker-compose.yml` / Podman Compose):

```json
{
  "Database": { "Provider": "Npgsql" },
  "ConnectionStrings": {
    "ScoreForgeDb": "Host=localhost;Port=5432;Database=scoreforge;Username=scoreforge;Password=scoreforge"
  }
}
```

DB helper:

```powershell
./scripts/db.ps1 -Action Start
./scripts/db.ps1 -Action Status
./scripts/db.ps1 -Action Stop
```

To use SQLite instead, set `"Database:Provider": "Sqlite"` and a `Data Source=...` connection string.

## Features (v1)

- Google / Microsoft login (plus Dev login in Development)
- Live multi-device match sync over SignalR
- Cribbage pegboard (61 / 121)
- American Canasta round scoresheet (to 5000)
- Versioned, idempotent score events with undo
