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
- Client local storage abstraction with in-memory implementation.
