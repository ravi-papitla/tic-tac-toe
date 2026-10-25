using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Services;

/// <summary>
/// In-memory game sessions plus the session-level scoreboard. Registered as a singleton.
/// A single lock serialises every operation, so a human move, the computer reply and the
/// scoreboard update always happen atomically.
/// </summary>
public sealed class GameService : IGameService
{
    private readonly object _sync = new();
    private readonly Dictionary<Guid, TicTacToeGame> _games = new();
    private readonly Scoreboard _scoreboard = new();

    public GameStateResponse CreateGame(GameMode mode)
    {
        lock (_sync)
        {
            var game = new TicTacToeGame(Guid.NewGuid(), mode);
            _games[game.Id] = game;
            return Map(game);
        }
    }

    public GameStateResponse GetGame(Guid id)
    {
        lock (_sync)
        {
            return Map(Find(id));
        }
    }

    public GameStateResponse PlayMove(Guid id, Player player, int cellIndex)
    {
        lock (_sync)
        {
            var game = Find(id);

            game.PlayMove(player, cellIndex); // throws (and changes nothing) if the move is invalid

            if (game.Mode == GameMode.VsComputer && game.Status == GameStatus.InProgress)
            {
                game.PlayComputerMove();
            }

            _scoreboard.Record(game); // no-op unless the game just completed; never counts twice
            return Map(game);
        }
    }

    public GameStateResponse Undo(Guid id)
    {
        lock (_sync)
        {
            var game = Find(id);
            game.Undo(); // Option A: only allowed while in progress, so the scoreboard never needs adjusting
            return Map(game);
        }
    }

    public GameStateResponse Reset(Guid id)
    {
        lock (_sync)
        {
            var game = Find(id);
            game.Reset(); // scoreboard intentionally untouched
            return Map(game);
        }
    }

    public ScoreboardResponse GetScoreboard()
    {
        lock (_sync)
        {
            return MapScoreboard();
        }
    }

    public ScoreboardResponse ResetScoreboard()
    {
        lock (_sync)
        {
            _scoreboard.Reset();
            return MapScoreboard();
        }
    }

    private TicTacToeGame Find(Guid id) =>
        _games.TryGetValue(id, out var game) ? game : throw new GameNotFoundException(id);

    private ScoreboardResponse MapScoreboard() => new(_scoreboard.XWins, _scoreboard.OWins, _scoreboard.Draws);

    private GameStateResponse Map(TicTacToeGame game) => new(
        game.Id,
        game.Board.ToArray(),
        game.Status == GameStatus.InProgress ? game.CurrentPlayer : null,
        game.Mode,
        game.Status,
        game.Winner,
        game.WinningCells.ToArray(),
        game.Moves
            .Select(m => new MoveResponse(m.Number, m.Player, m.Row, m.Column, m.CellIndex, $"Row {m.Row}, Column {m.Column}"))
            .ToArray(),
        game.CanUndo,
        MapScoreboard());
}
