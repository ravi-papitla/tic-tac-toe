using TicTacToe.Api.Contracts;
using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Services;

public interface IGameService
{
    GameStateResponse CreateGame(GameMode mode);
    GameStateResponse GetGame(Guid id);
    GameStateResponse PlayMove(Guid id, Player player, int cellIndex);
    GameStateResponse Undo(Guid id);
    GameStateResponse Reset(Guid id);
    ScoreboardResponse GetScoreboard();
    ScoreboardResponse ResetScoreboard();
}
