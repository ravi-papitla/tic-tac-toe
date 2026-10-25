# Tic Tac Toe — Angular + .NET

A browser-based Tic Tac Toe game. The **Angular** frontend talks to a **.NET 8 Web API** over REST; the backend owns the game session, rules, move history and scoreboard, and the frontend renders whatever state the backend returns.

## Tech stack

| Layer | Technology |
| --- | --- |
| Frontend | Angular 19 (standalone components, signals), TypeScript, Karma/Jasmine |
| Backend | .NET 8 Web API (controllers), C# 12, Swagger/OpenAPI |
| API style | REST + JSON (enums serialised as strings) |
| Storage | In-memory (singleton service, thread-safe) |
| Backend tests | xUnit (unit + `WebApplicationFactory` integration tests) |
| Source control | Git / GitHub (CI workflow in `.github/workflows/ci.yml`) |

## Repository layout

```
backend/
  TicTacToe.sln
  src/TicTacToe.Api/          Domain/ (rules)  Services/  Controllers/  Contracts/  Infrastructure/
  tests/TicTacToe.Tests/      xUnit tests
frontend/                     Angular app (src/app: components/, services/, models/)
docs/
  API.md                      Full API contract with examples
  AI_WORKFLOW.md              AI-assisted development notes and prompt summary
  REVIEW_NOTES.md             Talking points for the panel review
```

## Features implemented

- 3 × 3 board; empty cells clickable; a played cell shows **X** or **O** and stays locked.
- Two players (X, O) with alternating turns; current player clearly displayed; invalid moves never change the turn.
- Win detection (row, column, diagonal): winner shown, **winning cells highlighted**, board locked, scoreboard updated.
- Draw detection: draw message, board locked, scoreboard updated.
- **Reset Game**: clears board, history and status, current player back to X, scoreboard unchanged.
- **Move history** (move number, player, "Row r, Column c") updated after every valid move.
- **Undo Last Move**: one move in Two Player Mode; the computer's move **and** the human move before it in Computer Mode. Board, turn, status and history are restored; the button is disabled when there is nothing to undo.
- **Session scoreboard** (X wins, O wins, draws) served by the backend, counted **once** per completed game, with a separate **Reset Scoreboard**.
- **Two modes**: Two Player and Play Against Computer (human = X, computer = O). The computer plays automatically, only makes valid moves, never moves after the game ends, and follows the required priority: win → block → centre → corner → any cell.
- Backend validation of moves: outside the board, occupied cell, game already completed, wrong player.
- Bonus: the active game id is kept in `sessionStorage`, so a page refresh resumes the same backend session; Swagger UI for exploring the API.

## Prerequisites

- [.NET SDK 8.0 or newer](https://dotnet.microsoft.com/download) (the projects target `net8.0` and roll forward to newer runtimes)
- [Node.js](https://nodejs.org/) 20 or 22 LTS and npm

## Run the backend

```bash
cd backend/src/TicTacToe.Api
dotnet run
```

The API listens on **http://localhost:5080** (Swagger UI: http://localhost:5080/swagger). CORS allows the Angular dev server at `http://localhost:4200`.

## Run the frontend

In a second terminal:

```bash
cd frontend
npm install
npm start
```

Open **http://localhost:4200**. The frontend expects the API at `http://localhost:5080/api` (see `frontend/src/environments/environment.ts` if you change the port).

## Run the tests

```bash
# Backend (unit + integration)
cd backend
dotnet test

# Frontend (headless Chrome required; use "npm test" for an interactive watch run)
cd frontend
npm run test:ci
```

Backend tests cover every required scenario: valid/invalid moves (out of bounds, occupied, wrong player), turn switching, row/column/diagonal wins, draw, reset, undo in both modes, scoreboard updates (including "only once"), computer move selection (each priority rule plus 300 seeded random games proving it only plays free cells), moves after completion, and the REST contract (status codes, error codes, JSON shape). Frontend tests cover the API service (URLs, verbs, bodies, error mapping), the board component, and the main component against a mocked API.

## API endpoint summary

| Method | Endpoint | Purpose |
| --- | --- | --- |
| POST | `/api/games` | Create a game. Body: `{ "mode": "TwoPlayer" \| "VsComputer" }` (optional, default `TwoPlayer`) |
| GET | `/api/games/{id}` | Get current game state |
| POST | `/api/games/{id}/moves` | Submit a move. Body: `{ "player": "X" \| "O", "cellIndex": 0-8 }` |
| POST | `/api/games/{id}/undo` | Undo last move (or move pair in computer mode) |
| POST | `/api/games/{id}/reset` | Reset board/history; scoreboard unchanged |
| GET | `/api/scoreboard` | Get scoreboard |
| POST | `/api/scoreboard/reset` | Reset scoreboard |

Errors use RFC 7807 problem details with a `code` field: `400` (`OutOfBounds`, or malformed body), `404` (`GameNotFound`), `409` (`CellOccupied`, `GameCompleted`, `WrongPlayer`, `NothingToUndo`, `UndoNotAllowed`). Full request/response examples are in [docs/API.md](docs/API.md).

## AI tools and prompt summary

See [docs/AI_WORKFLOW.md](docs/AI_WORKFLOW.md) for the specification-first workflow, the prompts used, what the AI generated, what was reviewed or changed manually, and the assumptions made.

## Design decisions

- **Backend is the source of truth.** The frontend never computes wins, turns or computer moves; every action is a REST call and the UI renders the returned `GameStateResponse`. The response includes `canUndo` so the Undo rule lives in one place.
- **Pure rules engine + game aggregate + service.** `GameEngine` (win lines, computer move choice) is pure and stateless; `TicTacToeGame` enforces move/undo/reset rules and is trivially unit-testable; `GameService` holds sessions and the scoreboard behind a single lock so "human move → computer reply → score update" is atomic.
- **Cell index 0–8** (row-major) in the API; the response also carries 1-based `row`/`column` and a ready-made `position` string for the history table.
- **Score counted once** via a per-game `ScoreRecorded` flag, so repeated reads, rejected moves or retries can never double-count.
- **Computer replies inside the move request**, so the client never sees a half-finished turn and there is no polling.
- **Invalid moves are rejected with 4xx and leave state untouched**, so the turn never changes on an invalid move.
- **Mode switch creates a new game** (a game is bound to one mode); the scoreboard is session-wide and survives it.
- **Corner choice is deterministic** (first free of 0, 2, 6, 8), which keeps computer behaviour testable.

## Clarifications and assumptions

- **Clarification 2 (Scoreboard and Undo): Option A — Undo is disabled after a game is won or drawn.** The scoreboard is therefore final for a completed game and never needs adjusting. The backend rejects such an undo with `409 UndoNotAllowed` and reports `canUndo: false`.
- In computer mode a human move that ends the game (win or draw) gets no computer reply, so undo is only ever possible while the game is in progress, where moves come in human+computer pairs.
- "Reset Game" resets the *same* game id (fresh session state, same mode).
- "Session-level" scoreboard = one scoreboard for the running backend process, shared by all games (single-user local app). It resets when the backend restarts.
- Move request: the game id is in the URL (`/games/{id}/moves`) rather than the body.
- In computer mode, the human is always X and moves first.
- `currentPlayer` is `null` in the response once a game is completed.

## Known limitations

- State is in-memory: restarting the backend loses all games and the scoreboard.
- Finished/abandoned games are never evicted from memory (fine for a local exercise).
- One global scoreboard; there are no user accounts or per-user scoreboards.
- The computer follows the specified heuristic, not a minimax strategy, so it can be beaten (e.g. with a fork).
- The API has no authentication (local use only).

## Future improvements

- SQLite/EF Core persistence; evict stale sessions.
- Option B undo (allow undo after completion with scoreboard reversal).
- Selectable difficulty (minimax "unbeatable" mode); choose to play as O or move second.
- Per-session scoreboards, SignalR for online two-player play.
- Playwright end-to-end tests; Docker Compose for one-command start.
