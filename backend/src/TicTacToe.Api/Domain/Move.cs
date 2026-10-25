namespace TicTacToe.Api.Domain;

/// <summary>A single move. <see cref="CellIndex"/> is 0-8, row-major (0 = top-left, 8 = bottom-right).</summary>
public sealed record Move(int Number, Player Player, int CellIndex)
{
    /// <summary>1-based row, for display.</summary>
    public int Row => CellIndex / GameEngine.Size + 1;

    /// <summary>1-based column, for display.</summary>
    public int Column => CellIndex % GameEngine.Size + 1;
}
