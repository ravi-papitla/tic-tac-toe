using System.ComponentModel.DataAnnotations;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Contracts;

public sealed class CreateGameRequest
{
    /// <summary>TwoPlayer (default) or VsComputer.</summary>
    public GameMode Mode { get; init; } = GameMode.TwoPlayer;
}

public sealed class MoveRequest
{
    /// <summary>The player making the move: "X" or "O". The game id is taken from the URL.</summary>
    [Required]
    public Player? Player { get; init; }

    /// <summary>Cell index 0-8, row-major (0 = top-left, 8 = bottom-right).</summary>
    [Required]
    public int? CellIndex { get; init; }
}
