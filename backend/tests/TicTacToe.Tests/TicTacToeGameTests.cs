using TicTacToe.Api.Domain;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

public class TicTacToeGameTests
{
    // ---------- moves & turns ----------

    [Fact]
    public void New_game_is_empty_in_progress_and_x_starts()
    {
        var game = NewTwoPlayerGame();

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Empty(game.Moves);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Valid_move_marks_the_cell_and_records_history()
    {
        var game = NewTwoPlayerGame();

        game.PlayMove(Player.X, 4);

        Assert.Equal(Player.X, game.Board[4]);
        var move = Assert.Single(game.Moves);
        Assert.Equal(1, move.Number);
        Assert.Equal(Player.X, move.Player);
        Assert.Equal(2, move.Row);
        Assert.Equal(2, move.Column);
    }

    [Fact]
    public void Turns_alternate_after_every_valid_move()
    {
        var game = NewTwoPlayerGame();

        game.PlayMove(Player.X, 0);
        Assert.Equal(Player.O, game.CurrentPlayer);

        game.PlayMove(Player.O, 1);
        Assert.Equal(Player.X, game.CurrentPlayer);

        game.PlayMove(Player.X, 2);
        Assert.Equal(Player.O, game.CurrentPlayer);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(9)]
    [InlineData(100)]
    public void Move_outside_the_board_is_rejected_and_turn_does_not_change(int cell)
    {
        var game = NewTwoPlayerGame();

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.X, cell));

        Assert.Equal(GameErrorCode.OutOfBounds, ex.Code);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void Move_on_an_occupied_cell_is_rejected_and_turn_does_not_change()
    {
        var game = NewTwoPlayerGame();
        game.PlayMove(Player.X, 0);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameErrorCode.CellOccupied, ex.Code);
        Assert.Equal(Player.O, game.CurrentPlayer);
        Assert.Equal(Player.X, game.Board[0]);
        Assert.Single(game.Moves);
    }

    [Fact]
    public void Move_by_the_wrong_player_is_rejected_and_turn_does_not_change()
    {
        var game = NewTwoPlayerGame();

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.Empty(game.Moves);
    }

    [Fact]
    public void Human_cannot_play_as_o_in_computer_mode()
    {
        var game = NewComputerGame();

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
    }

    // ---------- win / draw ----------

    [Fact]
    public void Row_win_is_detected_with_winning_cells()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XRowWin);

        AssertWon(game, Player.X, 0, 1, 2);
    }

    [Fact]
    public void Column_win_is_detected_with_winning_cells()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XColumnWin);

        AssertWon(game, Player.X, 0, 3, 6);
    }

    [Fact]
    public void Diagonal_win_is_detected_with_winning_cells()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XDiagonalWin);

        AssertWon(game, Player.X, 0, 4, 8);
    }

    [Fact]
    public void Anti_diagonal_win_is_detected_with_winning_cells()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XAntiDiagonalWin);

        AssertWon(game, Player.X, 2, 4, 6);
    }

    [Fact]
    public void O_can_win_too()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, ORowWin);

        AssertWon(game, Player.O, 3, 4, 5);
    }

    [Fact]
    public void Full_board_without_a_line_is_a_draw()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, Draw);

        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(9, game.Moves.Count);
        Assert.True(game.IsCompleted);
    }

    [Fact]
    public void Winning_on_the_ninth_move_is_a_win_not_a_draw()
    {
        var game = NewTwoPlayerGame();

        PlayAll(game, 0, 1, 2, 3, 4, 5, 7, 6, 8); // X completes 0-4-8 with the last free cell

        AssertWon(game, Player.X, 0, 4, 8);
        Assert.Equal(9, game.Moves.Count);
    }

    [Fact]
    public void Moves_after_a_win_are_rejected()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XRowWin);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 8));

        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(5, game.Moves.Count);
        Assert.Null(game.Board[8]);
    }

    [Fact]
    public void Moves_after_a_draw_are_rejected()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, Draw);

        var ex = Assert.Throws<GameRuleException>(() => game.PlayMove(Player.O, 0));

        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(9, game.Moves.Count);
    }

    [Fact]
    public void Computer_cannot_move_after_the_game_is_completed()
    {
        var game = NewComputerGame();
        // X wins against the computer: X0 O4 X8 O2 X6 O7 X3
        foreach (var cell in new[] { 0, 8, 6, 3 })
        {
            game.PlayMove(Player.X, cell);
            if (!game.IsCompleted) game.PlayComputerMove();
        }

        Assert.Equal(GameStatus.Won, game.Status);
        var movesBefore = game.Moves.Count;

        var ex = Assert.Throws<GameRuleException>(() => game.PlayComputerMove());

        Assert.Equal(GameErrorCode.GameCompleted, ex.Code);
        Assert.Equal(movesBefore, game.Moves.Count);
    }

    // ---------- reset ----------

    [Fact]
    public void Reset_clears_board_history_status_and_returns_turn_to_x()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, XRowWin);

        game.Reset();

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Null(game.Winner);
        Assert.Empty(game.WinningCells);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.False(game.ScoreRecorded);
    }

    [Fact]
    public void Reset_mid_game_when_o_was_next_gives_x_the_first_move()
    {
        var game = NewTwoPlayerGame();
        game.PlayMove(Player.X, 0);

        game.Reset();

        Assert.Equal(Player.X, game.CurrentPlayer);
        game.PlayMove(Player.X, 0); // cell 0 is free again
    }

    // ---------- undo ----------

    [Fact]
    public void Undo_in_two_player_mode_removes_only_the_latest_move()
    {
        var game = NewTwoPlayerGame();
        game.PlayMove(Player.X, 0);
        game.PlayMove(Player.O, 4);

        game.Undo();

        Assert.Null(game.Board[4]);
        Assert.Equal(Player.X, game.Board[0]);
        Assert.Single(game.Moves);
        Assert.Equal(Player.O, game.CurrentPlayer); // O's move was removed, so it is O's turn again
        Assert.Equal(GameStatus.InProgress, game.Status);
    }

    [Fact]
    public void Undo_in_two_player_mode_can_go_back_to_an_empty_board()
    {
        var game = NewTwoPlayerGame();
        game.PlayMove(Player.X, 0);

        game.Undo();

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
        Assert.False(game.CanUndo);
    }

    [Fact]
    public void Undo_in_computer_mode_removes_the_computer_move_and_the_human_move_before_it()
    {
        var game = NewComputerGame();
        game.PlayMove(Player.X, 0);
        game.PlayComputerMove(); // O takes the centre

        game.Undo();

        Assert.All(game.Board, cell => Assert.Null(cell));
        Assert.Empty(game.Moves);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_in_computer_mode_only_removes_one_pair_at_a_time()
    {
        var game = NewComputerGame();
        game.PlayMove(Player.X, 0);
        game.PlayComputerMove(); // O4
        game.PlayMove(Player.X, 1);
        game.PlayComputerMove(); // O2 (blocks)
        Assert.Equal(4, game.Moves.Count);

        game.Undo();

        Assert.Equal(2, game.Moves.Count);
        Assert.Equal(Player.X, game.Board[0]);
        Assert.Equal(Player.O, game.Board[4]);
        Assert.Null(game.Board[1]);
        Assert.Null(game.Board[2]);
        Assert.Equal(Player.X, game.CurrentPlayer);
    }

    [Fact]
    public void Undo_with_no_moves_is_rejected()
    {
        var game = NewTwoPlayerGame();

        var ex = Assert.Throws<GameRuleException>(() => game.Undo());

        Assert.Equal(GameErrorCode.NothingToUndo, ex.Code);
    }

    [Fact]
    public void Undo_is_disabled_after_a_win_or_a_draw()
    {
        var won = NewTwoPlayerGame();
        PlayAll(won, XRowWin);
        var drawn = NewTwoPlayerGame();
        PlayAll(drawn, Draw);

        Assert.False(won.CanUndo);
        Assert.False(drawn.CanUndo);
        Assert.Equal(GameErrorCode.UndoNotAllowed, Assert.Throws<GameRuleException>(() => won.Undo()).Code);
        Assert.Equal(GameErrorCode.UndoNotAllowed, Assert.Throws<GameRuleException>(() => drawn.Undo()).Code);
    }

    [Fact]
    public void Undo_keeps_move_numbers_contiguous()
    {
        var game = NewTwoPlayerGame();
        PlayAll(game, 0, 1, 2);

        game.Undo();
        game.PlayMove(Player.X, 8);

        Assert.Equal(new[] { 1, 2, 3 }, game.Moves.Select(m => m.Number).ToArray());
        Assert.Equal(8, game.Moves[^1].CellIndex);
    }

    // ---------- helpers ----------

    private static void AssertWon(TicTacToeGame game, Player winner, params int[] line)
    {
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(winner, game.Winner);
        Assert.Equal(line, game.WinningCells);
        Assert.True(game.IsCompleted);
    }
}
