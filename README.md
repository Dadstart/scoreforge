# ScoreForge

Web-first game score-keeping application.

## Solution structure

- `src/ScoreForge.Client`: Blazor WebAssembly PWA client.
- `src/ScoreForge.Api`: Minimal API backend for auth/sync and real-time phases.
- `src/ScoreForge.Contracts`: Shared contracts and domain records.

## Phase 1 status

Phase 1 foundation scaffolding is in place:

- Shared domain contracts for user, game, scoreboard, participants, and events.
- API skeleton with health and foundation endpoints.
- Client local storage abstraction with IndexedDB implementation.

## Phase 2 status

Authentication plumbing is in place:

- API cookie authentication with external provider hooks for Google and Microsoft.
- Auth endpoints: `/api/auth/providers`, `/api/auth/me`, `/api/auth/login/{provider}`, `/api/auth/logout`.
- Client auth state provider and sign-in/sign-out UX shell.
- Foundation scoreboard endpoint requires an authenticated user.

## Configure social auth locally

Set provider credentials in `src/ScoreForge.Api/appsettings.Development.json`:

- `Authentication:Google:ClientId`
- `Authentication:Google:ClientSecret`
- `Authentication:Microsoft:ClientId`
- `Authentication:Microsoft:ClientSecret`
