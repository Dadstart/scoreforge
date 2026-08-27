using Dadstart.Labs.ScoreForge.Games.Abstractions;
using Dadstart.Labs.ScoreForge.Games.Cribbage;
using Xunit;

namespace Dadstart.Labs.ScoreForge.Games.Tests;

public sealed class CribbageEngineTests
{
    private readonly CribbageEngine _engine = new();

    [Fact]
    public void AddPoints_CapsAtWinningScore()
    {
        var seats = CreateSeats(2);
        var state = _engine.CreateInitialState(new MatchOptions(JsonStateHelper.SerializeToElement(new { winningScore = 121 })), seats);
        var seatId = seats[0].SeatId;

        state = _engine.Apply(state, Event("add_points", new { seatId, points = 100 }));
        state = _engine.Apply(state, Event("add_points", new { seatId, points = 30 }));

        var standings = _engine.GetStandings(state);
        Assert.Equal(121, standings.Seats[0].Score);
        Assert.True(standings.IsComplete);
        Assert.Equal(seatId, standings.WinnerSeatId);
    }

    [Fact]
    public void SetScore_UpdatesPreviousPeg()
    {
        var seats = CreateSeats(2);
        var state = _engine.CreateInitialState(new MatchOptions(JsonStateHelper.SerializeToElement(new { winningScore = 61 })), seats);
        var seatId = seats[1].SeatId;

        state = _engine.Apply(state, Event("add_points", new { seatId, points = 10 }));
        state = _engine.Apply(state, Event("set_score", new { seatId, score = 25 }));

        var standings = _engine.GetStandings(state);
        Assert.Equal(25, standings.Seats.First(s => s.SeatId == seatId).Score);
    }

    private static IReadOnlyList<SeatInfo> CreateSeats(int count) =>
        Enumerable.Range(0, count)
            .Select(i => new SeatInfo(Guid.NewGuid(), $"Player {i + 1}", null, i))
            .ToArray();

    private static ScoreEventInput Event(string type, object payload) =>
        new(Guid.NewGuid(), type, JsonStateHelper.SerializeToElement(payload), null, DateTimeOffset.UtcNow);
}
