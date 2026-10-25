namespace TicTacToe.Api.Domain;

public enum Player
{
    X,
    O
}

public enum GameMode
{
    TwoPlayer,
    VsComputer
}

public enum GameStatus
{
    InProgress,
    Won,
    Draw
}

public static class PlayerExtensions
{
    public static Player Opponent(this Player player) => player == Player.X ? Player.O : Player.X;
}
