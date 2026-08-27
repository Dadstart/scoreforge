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
    [InlineData(100, 1, 0, 0, false, false, 0, 600)]
    [InlineData(0, 0, 1, 2, true, false, 50, 550)]
    [InlineData(0, 0, 0, 0, true, true, 0, 200)]
    [InlineData(50, 0, 0, 0, false, true, 0, 250)]
    public void ComputeRoundScore_UsesAmericanCanastaValues(
        int cardPoints,
        int natural,
        int mixed,
        int redThrees,
        bool goingOut,
        bool goingOutBlind,
        int against,
        int expected)
    {
        var score = CanastaEngine.ComputeRoundScore(new CanastaEngine.RoundSideScore(
            "team-a", cardPoints, natural, mixed, redThrees, goingOut, against, goingOutBlind));
        Assert.Equal(expected, score);
    }

    [Fact]
    public void SubmitRound_GoingOutBlind_AddsTwoHundred()
    {
        var seats = TwoPlayerSeats();
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
                    new { teamId = "team-a", cardPoints = 0, naturalCanastas = 0, mixedCanastas = 0, redThrees = 0, goingOut = true, goingOutBlind = true, countsAgainst = 0 },
                    new { teamId = "team-b", cardPoints = 0, naturalCanastas = 0, mixedCanastas = 0, redThrees = 0, goingOut = false, goingOutBlind = false, countsAgainst = 0 }
                }
            }),
            null,
            DateTimeOffset.UtcNow));

        var standings = _engine.GetStandings(state);
        Assert.Equal(200, standings.Teams!.First(t => t.TeamId == "team-a").Score);
        Assert.Equal(0, standings.Teams!.First(t => t.TeamId == "team-b").Score);
    }

    [Fact]
    public void SubmitRound_RejectsTwoSidesGoingOut()
    {
        var seats = TwoPlayerSeats();
        var state = _engine.CreateInitialState(
            new MatchOptions(JsonStateHelper.SerializeToElement(new { winningScore = 5000 })),
            seats);

        var input = new ScoreEventInput(
            Guid.NewGuid(),
            CanastaEngine.SubmitRoundEvent,
            JsonStateHelper.SerializeToElement(new
            {
                sides = new[]
                {
                    new { teamId = "team-a", cardPoints = 0, naturalCanastas = 0, mixedCanastas = 0, redThrees = 0, goingOut = true, goingOutBlind = false, countsAgainst = 0 },
                    new { teamId = "team-b", cardPoints = 0, naturalCanastas = 0, mixedCanastas = 0, redThrees = 0, goingOut = false, goingOutBlind = true, countsAgainst = 0 }
                }
            }),
            null,
            DateTimeOffset.UtcNow);

        var ex = Assert.Throws<GameEngineException>(() => _engine.Apply(state, input));
        Assert.Equal("Only one side may go out in a round.", ex.Message);
    }

    private static SeatInfo[] TwoPlayerSeats() =>
    [
        new SeatInfo(Guid.NewGuid(), "Ann", "team-a", 0),
        new SeatInfo(Guid.NewGuid(), "Bob", "team-b", 1)
    ];
}
