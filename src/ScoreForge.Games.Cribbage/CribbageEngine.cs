using Dadstart.Labs.ScoreForge.Games.Abstractions;

namespace Dadstart.Labs.ScoreForge.Games.Cribbage;

public sealed class CribbageEngine : IGameEngine
{
    public const string AddPointsEvent = "add_points";
    public const string SetScoreEvent = "set_score";
    public const string ResetEvent = "reset";

    public string GameId => GameIds.Cribbage;

    public GameState CreateInitialState(MatchOptions options, IReadOnlyList<SeatInfo> seats)
    {
        if (seats.Count is < 2 or > 4)
            throw new GameEngineException("Cribbage requires 2 to 4 seats.");

        var winningScore = JsonStateHelper.TryGetProperty(options.Values ?? default, "winningScore", (int?)121) ?? 121;
        if (winningScore is not (61 or 121))
            throw new GameEngineException("Cribbage winning score must be 61 or 121.");

        var data = new CribbageStateData(
            winningScore,
            seats.Select(s => new CribbageSeatScore(s.SeatId, 0, null)).ToArray());

        return new GameState(
            GameId,
            seats,
            JsonStateHelper.SerializeToElement(new { winningScore }),
            JsonStateHelper.SerializeToElement(data));
    }

    public GameState Apply(GameState state, ScoreEventInput input)
    {
        var data = JsonStateHelper.Deserialize<CribbageStateData>(state.Data);

        var next = input.EventType switch
        {
            AddPointsEvent => ApplyAddPoints(data, input),
            SetScoreEvent => ApplySetScore(data, input),
            ResetEvent => ApplyReset(data, state.Seats),
            _ => throw new GameEngineException($"Unknown cribbage event type '{input.EventType}'.")
        };

        return state with { Data = JsonStateHelper.SerializeToElement(next) };
    }

    public MatchStandings GetStandings(GameState state)
    {
        var data = JsonStateHelper.Deserialize<CribbageStateData>(state.Data);
        var seats = state.Seats
            .Select(seat =>
            {
                var score = data.Scores.First(s => s.SeatId == seat.SeatId);
                return new SeatStanding(seat.SeatId, seat.DisplayName, seat.TeamId, score.Score);
            })
            .ToArray();

        var winner = seats.FirstOrDefault(s => s.Score >= data.WinningScore);
        var details = JsonStateHelper.SerializeToElement(new
        {
            winningScore = data.WinningScore,
            previousScores = data.Scores.ToDictionary(s => s.SeatId.ToString(), s => s.PreviousScore)
        });

        return new MatchStandings(seats, null, winner?.SeatId, null, winner is not null, details);
    }

    public bool IsComplete(GameState state) => GetStandings(state).IsComplete;

    private static CribbageStateData ApplyAddPoints(CribbageStateData data, ScoreEventInput input)
    {
        var payload = JsonStateHelper.Deserialize<AddPointsPayload>(input.Payload);
        if (payload.Points <= 0)
            throw new GameEngineException("Points must be positive.");

        var scores = data.Scores.ToList();
        var index = scores.FindIndex(s => s.SeatId == payload.SeatId);
        if (index < 0)
            throw new GameEngineException("Seat not found.");

        var current = scores[index];
        var updated = Math.Min(data.WinningScore, current.Score + payload.Points);
        scores[index] = current with { PreviousScore = current.Score, Score = updated };
        return data with { Scores = scores };
    }

    private static CribbageStateData ApplySetScore(CribbageStateData data, ScoreEventInput input)
    {
        var payload = JsonStateHelper.Deserialize<SetScorePayload>(input.Payload);
        if (payload.Score < 0 || payload.Score > data.WinningScore)
            throw new GameEngineException($"Score must be between 0 and {data.WinningScore}.");

        var scores = data.Scores.ToList();
        var index = scores.FindIndex(s => s.SeatId == payload.SeatId);
        if (index < 0)
            throw new GameEngineException("Seat not found.");

        var current = scores[index];
        if (current.Score == payload.Score)
            return data;

        scores[index] = current with { PreviousScore = current.Score, Score = payload.Score };
        return data with { Scores = scores };
    }

    private static CribbageStateData ApplyReset(CribbageStateData data, IReadOnlyList<SeatInfo> seats) =>
        new(data.WinningScore, seats.Select(s => new CribbageSeatScore(s.SeatId, 0, null)).ToArray());

    private sealed record CribbageStateData(int WinningScore, IReadOnlyList<CribbageSeatScore> Scores);

    private sealed record CribbageSeatScore(Guid SeatId, int Score, int? PreviousScore);

    private sealed record AddPointsPayload(Guid SeatId, int Points);

    private sealed record SetScorePayload(Guid SeatId, int Score);
}
