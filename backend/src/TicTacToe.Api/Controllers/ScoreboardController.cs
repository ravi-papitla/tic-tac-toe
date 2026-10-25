using Microsoft.AspNetCore.Mvc;
using TicTacToe.Api.Contracts;
using TicTacToe.Api.Services;

namespace TicTacToe.Api.Controllers;

[ApiController]
[Route("api/scoreboard")]
public sealed class ScoreboardController : ControllerBase
{
    private readonly IGameService _games;

    public ScoreboardController(IGameService games)
    {
        _games = games;
    }

    [HttpGet]
    public ActionResult<ScoreboardResponse> Get() => _games.GetScoreboard();

    [HttpPost("reset")]
    public ActionResult<ScoreboardResponse> Reset() => _games.ResetScoreboard();
}
