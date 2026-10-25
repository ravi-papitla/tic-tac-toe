namespace TicTacToe.Api.Domain;

public enum GameErrorCode
{
    OutOfBounds,
    CellOccupied,
    GameCompleted,
    WrongPlayer,
    NothingToUndo,
    UndoNotAllowed
}

/// <summary>A rule of the game was violated (invalid move, invalid undo, ...).</summary>
public sealed class GameRuleException : Exception
{
    public GameRuleException(GameErrorCode code, string message) : base(message)
    {
        Code = code;
    }

    public GameErrorCode Code { get; }
}

public sealed class GameNotFoundException : Exception
{
    public GameNotFoundException(Guid id) : base($"Game '{id}' was not found.")
    {
        GameId = id;
    }

    public Guid GameId { get; }
}
