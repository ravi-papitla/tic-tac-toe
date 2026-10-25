using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;
using static TicTacToe.Tests.TestHelpers;

namespace TicTacToe.Tests;

public class GameServiceTests
{
    private readonly GameService _service = new();

    /// <summary>Plays the cells in order in a two-player game, alternating X and O.</summary>
    private GameStateResponse PlayTwoPlayer(Guid id, params int[] cells)
    {
        GameStateResponse? state = null;
        var player = Player.X;
        foreach (var cell in cells)
        {
            state = _service.PlayMove(id, player, cell);
            player = player.Opponent();
        }

        return state!;
    }

    // ---------- create / state ----------

    [Fact]
    public void CreateGame_returns_a_fresh_state_for_the_requested_mode()
    {
        var state = _service.CreateGame(GameMode.VsComputer);

        Assert.NotEqual(Guid.Empty, state.GameId);
        Assert.Equal(GameMode.VsComputer, state.Mode);
        Assert.Equal(GameStatus.InProgress, state.Status);
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.Equal(9, state.Board.Count);
        Assert.All(state.Board, cell => Assert.Null(cell));
        Assert.Empty(state.Moves);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public void GetGame_returns_the_same_state_and_unknown_ids_are_not_found()
    {
        var created = _service.CreateGame(GameMode.TwoPlayer);
        _service.PlayMove(created.GameId, Player.X, 0);

        var fetched = _service.GetGame(created.GameId);

        Assert.Equal(Player.X, fetched.Board[0]);
        Assert.Throws<GameNotFoundException>(() => _service.GetGame(Guid.NewGuid()));
    }

    [Fact]
    public void Move_history_reports_number_player_and_position_after_every_move()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;

        _service.PlayMove(id, Player.X, 0);
        var state = _service.PlayMove(id, Player.O, 4);

        Assert.Collection(state.Moves,
            m =>
            {
                Assert.Equal(1, m.MoveNumber);
                Assert.Equal(Player.X, m.Player);
                Assert.Equal("Row 1, Column 1", m.Position);
            },
            m =>
            {
                Assert.Equal(2, m.MoveNumber);
                Assert.Equal(Player.O, m.Player);
                Assert.Equal("Row 2, Column 2", m.Position);
            });
    }

    [Fact]
    public void Won_state_exposes_winner_and_winning_cells_and_hides_the_current_player()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;

        var state = PlayTwoPlayer(id, XRowWin);

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, state.WinningCells);
        Assert.Null(state.CurrentPlayer);
        Assert.False(state.CanUndo);
    }

    // ---------- scoreboard ----------

    [Fact]
    public void Scoreboard_counts_x_wins_o_wins_and_draws()
    {
        var xWin = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(xWin, XRowWin);
        Assert.Equal(new ScoreboardResponse(1, 0, 0), _service.GetScoreboard());

        var oWin = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(oWin, ORowWin);
        Assert.Equal(new ScoreboardResponse(1, 1, 0), _service.GetScoreboard());

        var draw = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(draw, Draw);
        Assert.Equal(new ScoreboardResponse(1, 1, 1), _service.GetScoreboard());
    }

    [Fact]
    public void Scoreboard_is_included_in_the_game_state()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;

        var state = PlayTwoPlayer(id, XRowWin);

        Assert.Equal(new ScoreboardResponse(1, 0, 0), state.Scoreboard);
    }

    [Fact]
    public void Scoreboard_updates_only_once_per_completed_game()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(id, XRowWin);

        // Rejected moves, undo attempts, and re-reading the game must not add to the score.
        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 8));
        Assert.Throws<GameRuleException>(() => _service.Undo(id));
        _service.GetGame(id);
        _service.GetGame(id);

        Assert.Equal(new ScoreboardResponse(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void Scoreboard_does_not_change_while_a_game_is_in_progress()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;

        PlayTwoPlayer(id, 0, 1, 2);

        Assert.Equal(new ScoreboardResponse(0, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void ResetGame_keeps_the_scoreboard_and_a_replayed_game_counts_again()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(id, XRowWin);

        var afterReset = _service.Reset(id);

        Assert.Equal(new ScoreboardResponse(1, 0, 0), afterReset.Scoreboard);
        Assert.Equal(GameStatus.InProgress, afterReset.Status);
        Assert.Empty(afterReset.Moves);
        Assert.Equal(Player.X, afterReset.CurrentPlayer);

        PlayTwoPlayer(id, ORowWin);
        Assert.Equal(new ScoreboardResponse(1, 1, 0), _service.GetScoreboard());
    }

    [Fact]
    public void ResetScoreboard_zeroes_everything_and_does_not_recount_old_games()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(id, XRowWin);

        var scoreboard = _service.ResetScoreboard();

        Assert.Equal(new ScoreboardResponse(0, 0, 0), scoreboard);
        _service.GetGame(id); // the finished game must not be counted again
        Assert.Equal(new ScoreboardResponse(0, 0, 0), _service.GetScoreboard());
    }

    // ---------- invalid moves through the service ----------

    [Fact]
    public void Invalid_moves_do_not_change_the_state()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        _service.PlayMove(id, Player.X, 0);
        var before = _service.GetGame(id);

        Assert.Equal(GameErrorCode.OutOfBounds, Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 9)).Code);
        Assert.Equal(GameErrorCode.CellOccupied, Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 0)).Code);
        Assert.Equal(GameErrorCode.WrongPlayer, Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.X, 1)).Code);

        var after = _service.GetGame(id);
        Assert.Equal(before.Board, after.Board);
        Assert.Equal(before.CurrentPlayer, after.CurrentPlayer);
        Assert.Equal(before.Moves.Count, after.Moves.Count);
    }

    [Fact]
    public void Move_on_an_unknown_game_is_not_found()
    {
        Assert.Throws<GameNotFoundException>(() => _service.PlayMove(Guid.NewGuid(), Player.X, 0));
    }

    // ---------- computer mode ----------

    [Fact]
    public void Computer_replies_automatically_after_the_human_move()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;

        var state = _service.PlayMove(id, Player.X, 0);

        Assert.Equal(2, state.Moves.Count);
        Assert.Equal(Player.O, state.Moves[1].Player);
        Assert.Equal(4, state.Moves[1].CellIndex); // centre is free, no win/block needed
        Assert.Equal(Player.X, state.CurrentPlayer);
    }

    [Fact]
    public void Computer_blocks_a_human_threat_through_the_api_flow()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;
        _service.PlayMove(id, Player.X, 0); // O4

        var state = _service.PlayMove(id, Player.X, 1); // X threatens 2

        Assert.Equal(2, state.Moves[3].CellIndex);
        Assert.Equal(Player.O, state.Board[2]);
    }

    [Fact]
    public void Computer_wins_when_it_can_and_the_scoreboard_records_it()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;
        _service.PlayMove(id, Player.X, 0); // O4
        _service.PlayMove(id, Player.X, 1); // O2 (block)

        var state = _service.PlayMove(id, Player.X, 3); // O6 wins on 2-4-6

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.O, state.Winner);
        Assert.Equal(new[] { 2, 4, 6 }, state.WinningCells);
        Assert.Equal(new ScoreboardResponse(0, 1, 0), state.Scoreboard);
    }

    [Fact]
    public void Computer_does_not_move_after_the_human_wins()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;
        GameStateResponse state = null!;
        foreach (var cell in new[] { 0, 8, 6, 3 }) // X0 O4 X8 O2 X6 O7 X3 -> X wins on 0-3-6
        {
            state = _service.PlayMove(id, Player.X, cell);
        }

        Assert.Equal(GameStatus.Won, state.Status);
        Assert.Equal(Player.X, state.Winner);
        Assert.Equal(7, state.Moves.Count);
        Assert.Equal(Player.X, state.Moves[^1].Player); // last move is the human's: no computer reply
        Assert.Equal(new ScoreboardResponse(1, 0, 0), state.Scoreboard);

        Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.X, 1));
        Assert.Equal(7, _service.GetGame(id).Moves.Count);
    }

    [Fact]
    public void Human_cannot_submit_a_move_for_o_in_computer_mode()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;

        var ex = Assert.Throws<GameRuleException>(() => _service.PlayMove(id, Player.O, 0));

        Assert.Equal(GameErrorCode.WrongPlayer, ex.Code);
    }

    // ---------- undo through the service ----------

    [Fact]
    public void Undo_in_two_player_mode_removes_one_move()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(id, 0, 4);

        var state = _service.Undo(id);

        Assert.Single(state.Moves);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Null(state.Board[4]);
        Assert.True(state.CanUndo);
    }

    [Fact]
    public void Undo_in_computer_mode_removes_the_pair_and_disables_undo_on_an_empty_board()
    {
        var id = _service.CreateGame(GameMode.VsComputer).GameId;
        _service.PlayMove(id, Player.X, 0); // X0 O4

        var state = _service.Undo(id);

        Assert.Empty(state.Moves);
        Assert.All(state.Board, cell => Assert.Null(cell));
        Assert.Equal(Player.X, state.CurrentPlayer);
        Assert.False(state.CanUndo);
    }

    [Fact]
    public void Undo_after_completion_is_rejected_and_the_scoreboard_stays_final()
    {
        var id = _service.CreateGame(GameMode.TwoPlayer).GameId;
        PlayTwoPlayer(id, XRowWin);

        var ex = Assert.Throws<GameRuleException>(() => _service.Undo(id));

        Assert.Equal(GameErrorCode.UndoNotAllowed, ex.Code);
        Assert.Equal(new ScoreboardResponse(1, 0, 0), _service.GetScoreboard());
    }

    [Fact]
    public void Games_are_independent_sessions()
    {
        var first = _service.CreateGame(GameMode.TwoPlayer).GameId;
        var second = _service.CreateGame(GameMode.TwoPlayer).GameId;

        _service.PlayMove(first, Player.X, 0);

        Assert.Empty(_service.GetGame(second).Moves);
        Assert.Single(_service.GetGame(first).Moves);
    }
}
