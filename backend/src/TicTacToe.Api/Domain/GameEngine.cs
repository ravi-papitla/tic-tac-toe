namespace TicTacToe.Api.Domain;

public sealed record WinResult(Player Winner, IReadOnlyList<int> Line);

/// <summary>Pure, stateless game rules: win detection and the computer opponent's move choice.</summary>
public static class GameEngine
{
    public const int Size = 3;
    public const int CellCount = Size * Size;

    private static readonly int[][] Lines =
    {
        new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 }, // rows
        new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 }, // columns
        new[] { 0, 4, 8 }, new[] { 2, 4, 6 }                     // diagonals
    };

    private static readonly int[] Corners = { 0, 2, 6, 8 };
    private const int Center = 4;

    public static WinResult? FindWinner(IReadOnlyList<Player?> board)
    {
        foreach (var line in Lines)
        {
            var first = board[line[0]];
            if (first is not null && board[line[1]] == first && board[line[2]] == first)
            {
                return new WinResult(first.Value, line);
            }
        }

        return null;
    }

    public static bool IsFull(IReadOnlyList<Player?> board) => board.All(cell => cell is not null);

    /// <summary>
    /// Computer move priority:
    /// 1) win, 2) block the opponent's win, 3) centre, 4) first free corner (0, 2, 6, 8), 5) first free cell.
    /// </summary>
    public static int ChooseComputerMove(IReadOnlyList<Player?> board, Player computer = Player.O)
    {
        var emptyCells = Enumerable.Range(0, CellCount).Where(i => board[i] is null).ToList();
        if (emptyCells.Count == 0)
        {
            throw new InvalidOperationException("There are no free cells left on the board.");
        }

        var winningCell = FindCompletingCell(board, computer);
        if (winningCell.HasValue) return winningCell.Value;

        var blockingCell = FindCompletingCell(board, computer.Opponent());
        if (blockingCell.HasValue) return blockingCell.Value;

        if (board[Center] is null) return Center;

        foreach (var corner in Corners)
        {
            if (board[corner] is null) return corner;
        }

        return emptyCells[0];
    }

    /// <summary>Returns the empty cell that would complete a line for <paramref name="player"/>, if any.</summary>
    private static int? FindCompletingCell(IReadOnlyList<Player?> board, Player player)
    {
        foreach (var line in Lines)
        {
            var owned = 0;
            int? emptyCell = null;

            foreach (var index in line)
            {
                if (board[index] == player) owned++;
                else if (board[index] is null) emptyCell = index;
            }

            if (owned == 2 && emptyCell.HasValue) return emptyCell;
        }

        return null;
    }
}
