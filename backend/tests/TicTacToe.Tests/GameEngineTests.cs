using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

public class GameEngineTests
{
    [Theory]
    [InlineData("XXX......", Player.X, new[] { 0, 1, 2 })]
    [InlineData("...OOO...", Player.O, new[] { 3, 4, 5 })]
    [InlineData("......XXX", Player.X, new[] { 6, 7, 8 })]
    [InlineData("X..X..X..", Player.X, new[] { 0, 3, 6 })]
    [InlineData(".O..O..O.", Player.O, new[] { 1, 4, 7 })]
    [InlineData("..X..X..X", Player.X, new[] { 2, 5, 8 })]
    [InlineData("O...O...O", Player.O, new[] { 0, 4, 8 })]
    [InlineData("..X.X.X..", Player.X, new[] { 2, 4, 6 })]
    public void FindWinner_detects_every_row_column_and_diagonal(string board, Player winner, int[] line)
    {
        var result = GameEngine.FindWinner(TestHelpers.Board(board));

        Assert.NotNull(result);
        Assert.Equal(winner, result!.Winner);
        Assert.Equal(line, result.Line);
    }

    [Theory]
    [InlineData(".........")]
    [InlineData("XO.......")]
    [InlineData("XOXOXOOXO")] // full board, no line
    public void FindWinner_returns_null_when_nobody_has_a_line(string board)
    {
        Assert.Null(GameEngine.FindWinner(TestHelpers.Board(board)));
    }

    [Fact]
    public void Computer_wins_when_it_can_even_if_it_could_block()
    {
        // O can win at 5; X threatens to win at 2. Winning has priority.
        var board = TestHelpers.Board("XX." + "OO." + "...");

        Assert.Equal(5, GameEngine.ChooseComputerMove(board));
    }

    [Fact]
    public void Computer_blocks_when_x_can_win_next()
    {
        var board = TestHelpers.Board("XX." + "O.." + "...");

        Assert.Equal(2, GameEngine.ChooseComputerMove(board));
    }

    [Fact]
    public void Computer_takes_center_when_free_and_no_win_or_block()
    {
        var board = TestHelpers.Board("X.." + "..." + "...");

        Assert.Equal(4, GameEngine.ChooseComputerMove(board));
    }

    [Fact]
    public void Computer_takes_the_first_free_corner_when_center_is_taken()
    {
        Assert.Equal(0, GameEngine.ChooseComputerMove(TestHelpers.Board("..." + ".X." + "...")));
        Assert.Equal(2, GameEngine.ChooseComputerMove(TestHelpers.Board("O.." + ".X." + "...")));
    }

    [Fact]
    public void Computer_takes_any_free_cell_as_the_last_resort()
    {
        // Only cell 8 is free (X also threatens there, but it is the only legal choice either way).
        var board = TestHelpers.Board("XOX" + "OXO" + "OX.");

        Assert.Equal(8, GameEngine.ChooseComputerMove(board));
    }

    [Fact]
    public void Computer_move_throws_on_a_full_board()
    {
        var board = TestHelpers.Board("XOXOXOOXO");

        Assert.Throws<InvalidOperationException>(() => GameEngine.ChooseComputerMove(board));
    }

    [Fact]
    public void Computer_only_ever_picks_free_cells_across_many_random_games()
    {
        var random = new Random(12345);

        for (var gameNumber = 0; gameNumber < 300; gameNumber++)
        {
            var game = TestHelpers.NewComputerGame();

            while (!game.IsCompleted)
            {
                var free = Enumerable.Range(0, 9).Where(i => game.Board[i] is null).ToList();
                game.PlayMove(Player.X, free[random.Next(free.Count)]);

                if (!game.IsCompleted)
                {
                    var before = game.Moves.Count;
                    var computerMove = game.PlayComputerMove(); // would throw/overwrite if it picked a taken cell

                    Assert.Equal(Player.O, computerMove.Player);
                    Assert.Equal(before + 1, game.Moves.Count);
                }
            }

            Assert.Equal(game.Moves.Count, game.Board.Count(cell => cell is not null));
        }
    }
}
