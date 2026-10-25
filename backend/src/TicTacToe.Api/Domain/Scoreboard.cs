namespace TicTacToe.Api.Domain;

/// <summary>Session-level win/draw counters.</summary>
public sealed class Scoreboard
{
    public int XWins { get; private set; }
    public int OWins { get; private set; }
    public int Draws { get; private set; }

    /// <summary>
    /// Adds the result of a completed game. A game is counted at most once
    /// (tracked by <see cref="TicTacToeGame.ScoreRecorded"/>). Returns true if the score changed.
    /// </summary>
    public bool Record(TicTacToeGame game)
    {
        if (!game.IsCompleted || game.ScoreRecorded) return false;

        if (game.Status == GameStatus.Draw) Draws++;
        else if (game.Winner == Player.X) XWins++;
        else OWins++;

        game.ScoreRecorded = true;
        return true;
    }

    public void Reset()
    {
        XWins = 0;
        OWins = 0;
        Draws = 0;
    }
}
