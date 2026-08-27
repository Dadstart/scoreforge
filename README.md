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

Open http://127.0.0.1:5173 (or https with `-Https`; see [HTTPS](#https) below). When OAuth secrets are empty, Development exposes a **Developer** sign-in provider.

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

Register redirect URIs against the **Vite origin** (OAuth is proxied so cookies stay same-site):

| Provider | Redirect URI |
| --- | --- |
| Microsoft | `http://127.0.0.1:5173/signin-microsoft` |
| Google | `http://127.0.0.1:5173/signin-google` |

Also add the `http://localhost:5173/...` variants if you browse via `localhost` instead of `127.0.0.1`.

### HTTPS

Local dev uses HTTPS on the API by default (`https://127.0.0.1:7016`). The Vite dev server is HTTP unless you opt in.

**One-time setup** (trusts the ASP.NET dev cert and generates browser-trusted Vite certs via [mkcert](https://github.com/FiloSottile/mkcert)):

```powershell
winget install FiloSottile.mkcert   # if mkcert is not installed
./scripts/https.ps1 -Action Setup
```

**Start with HTTPS** (SPA at `https://127.0.0.1:5173`):

```powershell
./scripts/dev.ps1 -Action Start -Https
```

Without mkcert, `-Https` uses a self-signed Vite certificate; your browser will show a one-time security warning.

Update OAuth redirect URIs when using HTTPS:

| Provider | Redirect URI |
| --- | --- |
| Microsoft | `https://127.0.0.1:5173/signin-microsoft` |
| Google | `https://127.0.0.1:5173/signin-google` |

Check certificate status:

```powershell
./scripts/https.ps1 -Action Status
```

**Production / LAN:** build the SPA (`npm run build` in `src/ScoreForge.Web`) so the API serves static files, then run `./scripts/run-remote.ps1` with a real TLS certificate bound to Kestrel (ports 443 / 8443). Configure `Networking:PublicHttpsPort` and `Networking:RedirectHttpToHttps` in `appsettings.Production.json` when HTTPS is on a non-standard port.

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
