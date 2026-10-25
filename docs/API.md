# API contract

Base URL: `http://localhost:5080` · Content type: `application/json` · Enums are strings.
Interactive docs: `http://localhost:5080/swagger`.

Cells are indexed **0–8, row-major**:

```
0 | 1 | 2
3 | 4 | 5
6 | 7 | 8
```

## Types

**GameStateResponse**

| Field | Type | Notes |
| --- | --- | --- |
| `gameId` | guid | |
| `board` | `("X" \| "O" \| null)[9]` | |
| `currentPlayer` | `"X" \| "O" \| null` | `null` once the game is completed |
| `mode` | `"TwoPlayer" \| "VsComputer"` | |
| `status` | `"InProgress" \| "Won" \| "Draw"` | |
| `winner` | `"X" \| "O" \| null` | |
| `winningCells` | `int[]` | 3 indexes when won, otherwise empty |
| `moves` | `MoveResponse[]` | history of the current game |
| `canUndo` | bool | server-side Undo rule (in progress and at least one move) |
| `scoreboard` | `ScoreboardResponse` | |

**MoveResponse**: `{ moveNumber, player, row, column, cellIndex, position }` (`row`/`column` are 1-based; `position` is e.g. `"Row 1, Column 1"`).

**ScoreboardResponse**: `{ xWins, oWins, draws }`

**Problem response** (all errors): RFC 7807 `{ status, title, detail, code }`.

## Endpoints

### POST /api/games
Create a game. Body is optional.

```json
{ "mode": "VsComputer" }
```
`201 Created` + `Location` header + `GameStateResponse`. Unknown mode → `400`.

### GET /api/games/{id}
`200` + `GameStateResponse`; `404 GameNotFound`.

### POST /api/games/{id}/moves
```json
{ "player": "X", "cellIndex": 4 }
```
`200` + `GameStateResponse`. In `VsComputer` mode the response already contains the computer's reply (the computer plays O immediately, and only if the game is still in progress). The scoreboard is updated in the same call when the move completes the game.

| Status | `code` | When |
| --- | --- | --- |
| 400 | `OutOfBounds` | `cellIndex` < 0 or > 8 |
| 400 | — (validation) | missing/unknown `player`, missing `cellIndex` |
| 404 | `GameNotFound` | unknown id |
| 409 | `CellOccupied` | cell already taken |
| 409 | `GameCompleted` | game already won/drawn |
| 409 | `WrongPlayer` | not that player's turn (or a human submitting O in computer mode) |

Rejected moves never change the state or the turn.

### POST /api/games/{id}/undo
No body. Two-player: removes the latest move. Computer mode: removes the computer's move and the human move before it. Restores board, turn and history.

`200` + `GameStateResponse`; `404`; `409 NothingToUndo`; `409 UndoNotAllowed` (game completed — Option A).

### POST /api/games/{id}/reset
No body. Clears board, history and status; current player = X; same id and mode; **scoreboard unchanged**. `200` + `GameStateResponse`; `404`.

### GET /api/scoreboard
`200` `{ "xWins": 1, "oWins": 0, "draws": 2 }`

### POST /api/scoreboard/reset
`200` `{ "xWins": 0, "oWins": 0, "draws": 0 }`

## Example: X wins the top row (two-player)

```
POST /api/games                      → 201  { gameId: "…", currentPlayer: "X", status: "InProgress", … }
POST /api/games/{id}/moves  X → 0    → 200  currentPlayer: "O"
POST /api/games/{id}/moves  O → 3    → 200
POST /api/games/{id}/moves  X → 1    → 200
POST /api/games/{id}/moves  O → 4    → 200
POST /api/games/{id}/moves  X → 2    → 200  status: "Won", winner: "X", winningCells: [0,1,2],
                                            currentPlayer: null, canUndo: false, scoreboard.xWins: 1
POST /api/games/{id}/moves  O → 8    → 409  code: "GameCompleted"
POST /api/games/{id}/undo            → 409  code: "UndoNotAllowed"
```
