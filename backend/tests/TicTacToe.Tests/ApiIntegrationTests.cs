using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Tests;

/// <summary>End-to-end tests of the REST contract using an in-memory test server.</summary>
public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        // Each test class instance shares one server; tests below only use their own game ids
        // and reset the scoreboard at the start of any scoreboard assertion.
        _client = factory.CreateClient();
    }

    private async Task<GameStateResponse> CreateGameAsync(GameMode mode = GameMode.TwoPlayer)
    {
        var response = await _client.PostAsJsonAsync("/api/games", new { mode = mode.ToString() });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        return (await response.Content.ReadFromJsonAsync<GameStateResponse>(Json))!;
    }

    private Task<HttpResponseMessage> MoveAsync(Guid id, string player, int cell) =>
        _client.PostAsJsonAsync($"/api/games/{id}/moves", new { player, cellIndex = cell });

    [Fact]
    public async Task Create_then_get_returns_the_same_new_game()
    {
        var created = await CreateGameAsync();

        var fetched = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{created.GameId}", Json);

        Assert.Equal(created.GameId, fetched!.GameId);
        Assert.Equal(GameStatus.InProgress, fetched.Status);
        Assert.Equal(GameMode.TwoPlayer, fetched.Mode);
    }

    [Fact]
    public async Task Create_without_a_body_defaults_to_two_player_mode()
    {
        var response = await _client.PostAsync("/api/games", null);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(GameMode.TwoPlayer, state!.Mode);
    }

    [Fact]
    public async Task Enums_are_serialised_as_strings()
    {
        var created = await CreateGameAsync();
        var response = await MoveAsync(created.GameId, "X", 0);

        var raw = await response.Content.ReadAsStringAsync();

        Assert.Contains("\"status\":\"InProgress\"", raw);
        Assert.Contains("\"currentPlayer\":\"O\"", raw);
        Assert.Contains("\"mode\":\"TwoPlayer\"", raw);
    }

    [Fact]
    public async Task Valid_move_returns_200_and_updated_state()
    {
        var created = await CreateGameAsync();

        var response = await MoveAsync(created.GameId, "X", 4);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var state = await response.Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(Player.X, state!.Board[4]);
        Assert.Equal(Player.O, state.CurrentPlayer);
        Assert.Single(state.Moves);
    }

    [Theory]
    [InlineData(9, HttpStatusCode.BadRequest, "OutOfBounds")]
    [InlineData(-1, HttpStatusCode.BadRequest, "OutOfBounds")]
    public async Task Move_outside_the_board_returns_400(int cell, HttpStatusCode expected, string code)
    {
        var created = await CreateGameAsync();

        var response = await MoveAsync(created.GameId, "X", cell);

        Assert.Equal(expected, response.StatusCode);
        Assert.Contains(code, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Move_on_an_occupied_cell_returns_409_and_keeps_the_turn()
    {
        var created = await CreateGameAsync();
        await MoveAsync(created.GameId, "X", 0);

        var response = await MoveAsync(created.GameId, "O", 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("CellOccupied", await response.Content.ReadAsStringAsync());
        var state = await _client.GetFromJsonAsync<GameStateResponse>($"/api/games/{created.GameId}", Json);
        Assert.Equal(Player.O, state!.CurrentPlayer);
    }

    [Fact]
    public async Task Move_by_the_wrong_player_returns_409()
    {
        var created = await CreateGameAsync();

        var response = await MoveAsync(created.GameId, "O", 0);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("WrongPlayer", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Move_with_an_unknown_player_or_missing_cell_returns_400()
    {
        var created = await CreateGameAsync();

        var badPlayer = await MoveAsync(created.GameId, "Z", 0);
        var missingCell = await _client.PostAsJsonAsync($"/api/games/{created.GameId}/moves", new { player = "X" });

        Assert.Equal(HttpStatusCode.BadRequest, badPlayer.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCell.StatusCode);
    }

    [Fact]
    public async Task Unknown_game_returns_404()
    {
        var get = await _client.GetAsync($"/api/games/{Guid.NewGuid()}");
        var move = await MoveAsync(Guid.NewGuid(), "X", 0);
        var undo = await _client.PostAsync($"/api/games/{Guid.NewGuid()}/undo", null);
        var reset = await _client.PostAsync($"/api/games/{Guid.NewGuid()}/reset", null);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, move.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, undo.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, reset.StatusCode);
    }

    [Fact]
    public async Task Full_game_win_undo_rules_scoreboard_and_resets_over_http()
    {
        await _client.PostAsync("/api/scoreboard/reset", null);
        var created = await CreateGameAsync();
        var id = created.GameId;

        // Undo works while in progress
        await MoveAsync(id, "X", 8);
        var undone = await (await _client.PostAsync($"/api/games/{id}/undo", null)).Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Empty(undone!.Moves);
        Assert.Equal(Player.X, undone.CurrentPlayer);

        // X wins the top row
        GameStateResponse? last = null;
        var players = new[] { "X", "O", "X", "O", "X" };
        var cells = new[] { 0, 3, 1, 4, 2 };
        for (var i = 0; i < cells.Length; i++)
        {
            last = await (await MoveAsync(id, players[i], cells[i])).Content.ReadFromJsonAsync<GameStateResponse>(Json);
        }

        Assert.Equal(GameStatus.Won, last!.Status);
        Assert.Equal(Player.X, last.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, last.WinningCells);
        Assert.Equal(1, last.Scoreboard.XWins);

        // Undo and further moves are rejected after completion
        var undoAfter = await _client.PostAsync($"/api/games/{id}/undo", null);
        var moveAfter = await MoveAsync(id, "O", 8);
        Assert.Equal(HttpStatusCode.Conflict, undoAfter.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, moveAfter.StatusCode);

        // Scoreboard endpoint matches and counted once
        var scoreboard = await _client.GetFromJsonAsync<ScoreboardResponse>("/api/scoreboard", Json);
        Assert.Equal(new ScoreboardResponse(1, 0, 0), scoreboard);

        // Reset Game keeps the scoreboard
        var reset = await (await _client.PostAsync($"/api/games/{id}/reset", null)).Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(GameStatus.InProgress, reset!.Status);
        Assert.Empty(reset.Moves);
        Assert.Equal(1, reset.Scoreboard.XWins);

        // Reset Scoreboard zeroes it
        var cleared = await (await _client.PostAsync("/api/scoreboard/reset", null)).Content.ReadFromJsonAsync<ScoreboardResponse>(Json);
        Assert.Equal(new ScoreboardResponse(0, 0, 0), cleared);
    }

    [Fact]
    public async Task Computer_mode_replies_and_undo_removes_the_pair_over_http()
    {
        var created = await CreateGameAsync(GameMode.VsComputer);
        var id = created.GameId;

        var afterMove = await (await MoveAsync(id, "X", 0)).Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Equal(2, afterMove!.Moves.Count);
        Assert.Equal(Player.O, afterMove.Moves[1].Player);

        var afterUndo = await (await _client.PostAsync($"/api/games/{id}/undo", null)).Content.ReadFromJsonAsync<GameStateResponse>(Json);
        Assert.Empty(afterUndo!.Moves);
        Assert.Equal(Player.X, afterUndo.CurrentPlayer);
        Assert.False(afterUndo.CanUndo);
    }
}
