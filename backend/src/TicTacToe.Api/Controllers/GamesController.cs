using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/games")]
public sealed class GamesController : ControllerBase
{
    private readonly IGameService _games;

    public GamesController(IGameService games)
    {
        _games = games;
    }

    /// <summary>Create a new game session (TwoPlayer by default).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(GameStateResponse), StatusCodes.Status201Created)]
    public ActionResult<GameStateResponse> Create([FromBody] CreateGameRequest? request)
    {
        var state = _games.CreateGame(request?.Mode ?? GameMode.TwoPlayer);
        return CreatedAtAction(nameof(Get), new { id = state.GameId }, state);
    }

    /// <summary>Get the current state of a game.</summary>
    [HttpGet("{id:guid}")]
    public ActionResult<GameStateResponse> Get(Guid id) => _games.GetGame(id);

    /// <summary>Submit a move. In computer mode the response already includes the computer's reply.</summary>
    [HttpPost("{id:guid}/moves")]
    public ActionResult<GameStateResponse> SubmitMove(Guid id, [FromBody] MoveRequest request) =>
        _games.PlayMove(id, request.Player!.Value, request.CellIndex!.Value);

    /// <summary>Undo the last move (two-player) or the last move pair (computer mode).</summary>
    [HttpPost("{id:guid}/undo")]
    public ActionResult<GameStateResponse> Undo(Guid id) => _games.Undo(id);

    /// <summary>Reset the board and history of this game. The scoreboard is unchanged.</summary>
    [HttpPost("{id:guid}/reset")]
    public ActionResult<GameStateResponse> Reset(Guid id) => _games.Reset(id);
}
