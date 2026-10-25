using TicTacToe.Api.Domain;

namespace TicTacToe.Api.Contracts;

public sealed record MoveResponse(
    int MoveNumber,
    Player Player,
    int Row,
    int Column,
    int CellIndex,
    string Position);

public sealed record ScoreboardResponse(int XWins, int OWins, int Draws);

/// <summary>Everything the frontend needs to render a game. CurrentPlayer is null once the game is completed.</summary>
public sealed record GameStateResponse(
    Guid GameId,
    IReadOnlyList<Player?> Board,
    Player? CurrentPlayer,
    GameMode Mode,
    GameStatus Status,
    Player? Winner,
    IReadOnlyList<int> WinningCells,
    IReadOnlyList<MoveResponse> Moves,
    bool CanUndo,
    ScoreboardResponse Scoreboard);
