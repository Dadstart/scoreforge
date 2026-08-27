namespace Dadstart.Labs.ScoreForge.Games.Abstractions;

public sealed class GameEngineException : Exception
{
    public GameEngineException(string message)
        : base(message)
    {
    }
}
