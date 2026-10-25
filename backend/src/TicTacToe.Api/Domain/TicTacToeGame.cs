namespace TicTacToe.Api.Domain;

/// <summary>
/// One game session. Owns the board, move history and status, and enforces every move/undo rule.
/// Not thread-safe on its own: <see cref="Services.GameService"/> serialises access.
/// </summary>
public sealed class TicTacToeGame
{
    private readonly Player?[] _board = new Player?[GameEngine.CellCount];
    private readonly List<Move> _moves = new();

    public TicTacToeGame(Guid id, GameMode mode)
    {
        Id = id;
        Mode = mode;
    }

    public Guid Id { get; }
    public GameMode Mode { get; }
    public IReadOnlyList<Player?> Board => Array.AsReadOnly(_board);
    public IReadOnlyList<Move> Moves => _moves;
    public Player CurrentPlayer { get; private set; } = Player.X;
    public GameStatus Status { get; private set; } = GameStatus.InProgress;
    public Player? Winner { get; private set; }
    public IReadOnlyList<int> WinningCells { get; private set; } = Array.Empty<int>();

    /// <summary>True once this game's result has been added to the scoreboard (guarantees "count once").</summary>
    public bool ScoreRecorded { get; internal set; }

    public bool IsCompleted => Status != GameStatus.InProgress;

    /// <summary>Undo policy "Option A": allowed only while the game is in progress and has moves.</summary>
    public bool CanUndo => !IsCompleted && _moves.Count > 0;

    /// <summary>Applies a move requested by a user. Throws <see cref="GameRuleException"/> if it is invalid.</summary>
    public void PlayMove(Player player, int cellIndex)
    {
        if (IsCompleted)
            throw new GameRuleException(GameErrorCode.GameCompleted, "The game is already over. Reset the game to play again.");

        if (cellIndex < 0 || cellIndex >= GameEngine.CellCount)
            throw new GameRuleException(GameErrorCode.OutOfBounds, $"Cell {cellIndex} is outside the board (valid cells are 0-{GameEngine.CellCount - 1}).");

        if (Mode == GameMode.VsComputer && player != Player.X)
            throw new GameRuleException(GameErrorCode.WrongPlayer, "In computer mode you play as X; the computer controls O.");

        if (player != CurrentPlayer)
            throw new GameRuleException(GameErrorCode.WrongPlayer, $"It is Player {CurrentPlayer}'s turn.");

        if (_board[cellIndex] is not null)
            throw new GameRuleException(GameErrorCode.CellOccupied, $"Cell {cellIndex} is already taken.");

        Place(player, cellIndex);
    }

    /// <summary>Lets the computer (always O) pick and play its move using <see cref="GameEngine.ChooseComputerMove"/>.</summary>
    public Move PlayComputerMove()
    {
        if (Mode != GameMode.VsComputer)
            throw new InvalidOperationException("The computer only plays in VsComputer mode.");

        if (IsCompleted)
            throw new GameRuleException(GameErrorCode.GameCompleted, "The game is already over; the computer cannot move.");

        if (CurrentPlayer != Player.O)
            throw new GameRuleException(GameErrorCode.WrongPlayer, "It is not the computer's turn.");

        var cell = GameEngine.ChooseComputerMove(_board, Player.O);
        Place(Player.O, cell);
        return _moves[^1];
    }

    /// <summary>
    /// Two-player mode: removes the latest move.
    /// Computer mode: removes the computer's latest move together with the human move before it.
    /// </summary>
    public void Undo()
    {
        if (IsCompleted)
            throw new GameRuleException(GameErrorCode.UndoNotAllowed, "Undo is disabled once a game has been won or drawn.");

        if (_moves.Count == 0)
            throw new GameRuleException(GameErrorCode.NothingToUndo, "There are no moves to undo.");

        var removed = RemoveLastMove();
        if (Mode == GameMode.VsComputer && removed.Player == Player.O && _moves.Count > 0)
        {
            removed = RemoveLastMove();
        }

        CurrentPlayer = removed.Player; // whoever made the earliest removed move plays again
        UpdateStatus();                 // recalculates win / draw from the restored board
    }

    /// <summary>Starts a fresh game in the same session (same id and mode). The scoreboard is not touched.</summary>
    public void Reset()
    {
        Array.Clear(_board);
        _moves.Clear();
        CurrentPlayer = Player.X;
        ScoreRecorded = false;
        UpdateStatus();
    }

    private void Place(Player player, int cellIndex)
    {
        _board[cellIndex] = player;
        _moves.Add(new Move(_moves.Count + 1, player, cellIndex));
        CurrentPlayer = player.Opponent();
        UpdateStatus();
    }

    private Move RemoveLastMove()
    {
        var last = _moves[^1];
        _moves.RemoveAt(_moves.Count - 1);
        _board[last.CellIndex] = null;
        return last;
    }

    private void UpdateStatus()
    {
        var win = GameEngine.FindWinner(_board);
        if (win is not null)
        {
            Status = GameStatus.Won;
            Winner = win.Winner;
            WinningCells = win.Line;
        }
        else
        {
            Status = GameEngine.IsFull(_board) ? GameStatus.Draw : GameStatus.InProgress;
            Winner = null;
            WinningCells = Array.Empty<int>();
        }
    }
}
