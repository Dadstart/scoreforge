using Dadstart.Labs.ScoreForge.Games.Abstractions;
using Dadstart.Labs.ScoreForge.Games.Canasta;
using Xunit;

namespace Dadstart.Labs.ScoreForge.Games.Tests;

public sealed class CanastaEngineTests
{
    private readonly CanastaEngine _engine = new();

    [Fact]
    public void SubmitRound_AddsPartnershipTotals()
    {
        var seats = new[]
        {
            new SeatInfo(Guid.NewGuid(), "Ann", "team-a", 0),
            new SeatInfo(Guid.NewGuid(), "Bob", "team-a", 1),
            new SeatInfo(Guid.NewGuid(), "Cara", "team-b", 2),
            new SeatInfo(Guid.NewGuid(), "Dan", "team-b", 3)
        };

        var state = _engine.CreateInitialState(
            new MatchOptions(JsonStateHelper.SerializeToElement(new { winningScore = 5000 })),
            seats);

        state = _engine.Apply(state, new ScoreEventInput(
            Guid.NewGuid(),
            CanastaEngine.SubmitRoundEvent,
            JsonStateHelper.SerializeToElement(new
            {
                sides = new[]
                {
                    new { teamId = "team-a", cardPoints = 200, naturalCanastas = 1, mixedCanastas = 0, redThrees = 1, goingOut = true, countsAgainst = 0 },
                    new { teamId = "team-b", cardPoints = 50, naturalCanastas = 0, mixedCanastas = 1, redThrees = 0, goingOut = false, countsAgainst = 20 }
                }
            }),
            null,
            DateTimeOffset.UtcNow));

        var standings = _engine.GetStandings(state);
        Assert.Equal(900, standings.Teams!.First(t => t.TeamId == "team-a").Score);
        Assert.Equal(330, standings.Teams!.First(t => t.TeamId == "team-b").Score);
        Assert.Equal(50, CanastaEngine.GetMeldThreshold(0));
        Assert.Equal(90, CanastaEngine.GetMeldThreshold(1500));
        Assert.Equal(120, CanastaEngine.GetMeldThreshold(3000));
    }

    [Theory]
    [InlineData(100, 1, 0, 0, false, 0, 600)]
    [InlineData(0, 0, 1, 2, true, 50, 550)]
    public void ComputeRoundScore_UsesAmericanCanastaValues(
        int cardPoints,
        int natural,
        int mixed,
        int redThrees,
        bool goingOut,
        int against,
        int expected)
    {
        var score = CanastaEngine.ComputeRoundScore(new CanastaEngine.RoundSideScore(
            "team-a", cardPoints, natural, mixed, redThrees, goingOut, against));
        Assert.Equal(expected, score);
    }
}
