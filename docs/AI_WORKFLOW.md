# AI-assisted development notes

> **Before submitting, edit this file so it describes what *you* actually did.** The panel will ask about your prompts, what the AI produced, and what you changed. Sections marked ✏️ need your own words.

## Tool used
Claude (Anthropic) was used to generate the first full draft of the solution from the problem statement.

## 1. From requirement to specification
The problem statement was turned into a short spec before any code was written:

1. **Domain model:** `Player {X,O}`, `GameMode {TwoPlayer, VsComputer}`, `GameStatus {InProgress, Won, Draw}`, `Move`, `TicTacToeGame`, `Scoreboard`.
2. **Rules as testable statements:** e.g. "invalid move → error and no state change", "score recorded once per completed game", "undo in computer mode removes O's move and the preceding X move".
3. **API contract:** the suggested endpoints, plus the `GameStateResponse` fields the statement lists, plus a server-side `canUndo` flag.
4. **Decisions on ambiguities** (documented in the README): Undo Option A; reset keeps same game id; scoreboard is process-wide; cell index instead of row/column.
5. **Test list** copied from the "Testing Expectations" section and mapped to test classes.

## 2. Prompts (summary)
1. *"Here is the problem statement for a Tic Tac Toe app (Angular + .NET). Complete the whole assignment: every functional requirement, must-have, API, tests, README. Everything should be perfect."* (attached the statement)
2. ✏️ Follow-up prompts you used (e.g. "explain why you chose Option A", "add a test for …", "change the UI colours").

## 3. What the AI generated
- .NET 8 Web API: domain (`GameEngine`, `TicTacToeGame`, `Scoreboard`), `GameService`, controllers, exception filter, Swagger/CORS setup.
- xUnit unit tests and `WebApplicationFactory` integration tests.
- Angular app: board, move history, scoreboard components, API service, main component, specs.
- README, API contract, CI workflow.

## 4. What I changed manually
✏️ List your own edits here (naming, styling, rules you adjusted, bugs you fixed).

## 5. Parts I reviewed most carefully
- `TicTacToeGame.PlayMove` validation order and `Undo` (pair removal in computer mode, turn restoration).
- `Scoreboard.Record` / `ScoreRecorded` (count-once guarantee) and `GameService.PlayMove` (human move → computer reply → score, under one lock).
- `GameEngine.ChooseComputerMove` priority order.
- Error mapping in `GameExceptionFilter` (400 vs 404 vs 409).
- ✏️ Anything else you checked.

## 6. Assumptions
See "Clarifications and assumptions" in the README.

## 7. Trade-offs
- In-memory storage (simple to run) vs SQLite (persistence) → chose in-memory.
- Undo Option A (simple, scoreboard always final) vs Option B (more flexible, needs score reversal) → chose A.
- Computer reply inside the move request (one round trip, atomic) vs separate endpoint/polling → chose inline.
- Heuristic computer (as specified) vs minimax → followed the specified priority list.
