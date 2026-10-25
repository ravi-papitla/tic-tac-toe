using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

internal static class TestHelpers
{
    // Move sequences (cell indexes, X always first). Cells: 0 1 2 / 3 4 5 / 6 7 8
    public static readonly int[] XRowWin = { 0, 3, 1, 4, 2 };        // X: 0,1,2
    public static readonly int[] XColumnWin = { 0, 1, 3, 2, 6 };     // X: 0,3,6
    public static readonly int[] XDiagonalWin = { 0, 1, 4, 2, 8 };   // X: 0,4,8
    public static readonly int[] XAntiDiagonalWin = { 2, 0, 4, 1, 6 }; // X: 2,4,6
    public static readonly int[] ORowWin = { 0, 3, 1, 4, 8, 5 };     // O: 3,4,5
    public static readonly int[] Draw = { 0, 1, 2, 4, 3, 5, 7, 6, 8 };

    /// <summary>Plays the cells in order in a two-player game; each move is made by whoever's turn it is.</summary>
    public static void PlayAll(TicTacToeGame game, params int[] cells)
    {
        foreach (var cell in cells)
        {
            game.PlayMove(game.CurrentPlayer, cell);
        }
    }

    public static TicTacToeGame NewTwoPlayerGame() => new(Guid.NewGuid(), GameMode.TwoPlayer);

    public static TicTacToeGame NewComputerGame() => new(Guid.NewGuid(), GameMode.VsComputer);

    /// <summary>Builds a board from 9 characters: 'X', 'O' or '.' (row-major).</summary>
    public static Player?[] Board(string cells)
    {
        if (cells.Length != 9) throw new ArgumentException("A board needs exactly 9 characters.", nameof(cells));

        return cells.Select(c => c switch
        {
            'X' => (Player?)Player.X,
            'O' => Player.O,
            '.' => null,
            _ => throw new ArgumentException($"Unexpected character '{c}'.", nameof(cells))
        }).ToArray();
    }
}
