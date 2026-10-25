# Panel review notes

## 60-second pitch
Angular renders; .NET decides. Every click is a REST call; the backend validates, applies the move, lets the computer reply (in computer mode), records the score when a game ends, and returns one `GameStateResponse`. The UI just displays it.

## Demo script (≈5 min)
1. Start API (`dotnet run`) and UI (`npm start`).
2. Two Player: play X/O to a row win → winning cells highlighted, scoreboard +1, board locked, Undo disabled.
3. Try an occupied cell via Swagger → `409 CellOccupied`, turn unchanged.
4. Reset Game → board cleared, scoreboard unchanged. Reset Scoreboard → zeros.
5. Draw a game → draw message, Draws +1.
6. Computer mode: play one move → computer replies instantly; Undo → both moves vanish, X to play.
7. Refresh the page → same game resumes (session id in `sessionStorage`).

## Likely questions
- **Why does the backend own state?** One source of truth; rules can't diverge; UI is replaceable.
- **Why 409 vs 400?** 400 = malformed/out-of-range request; 409 = valid request that conflicts with current state (occupied, game over, wrong turn).
- **How is the score counted once?** `ScoreRecorded` flag per game, checked inside the service lock.
- **Why Option A for undo?** Keeps the scoreboard final and the model simple; Option B would need score reversal and re-recording after redo.
- **Concurrency?** A single lock in `GameService`; fine for a local app, would move to per-game locks / a DB for scale.
- **Computer move order?** win → block → centre → first free corner (0,2,6,8) → first free cell. Deterministic for testing; not unbeatable (fork).
- **Why can't the fallback "any cell" step really trigger?** With centre and all corners taken and no win, a threat always exists, so win/block fires first; the step is kept for completeness/spec compliance.
- **What would you improve?** See "Future improvements" in the README.
