using Dadstart.Labs.ScoreForge.Games.Abstractions;

namespace Dadstart.Labs.ScoreForge.Games.Canasta;

public sealed class CanastaEngine : IGameEngine
{
    public const string SubmitRoundEvent = "submit_round";
    public const string ResetEvent = "reset";

    public const int NaturalCanastaPoints = 500;
    public const int MixedCanastaPoints = 300;
    public const int RedThreePoints = 100;
    public const int GoingOutPoints = 100;
    public const int GoingOutBlindPoints = 200;

    public string GameId => GameIds.Canasta;

    public GameState CreateInitialState(MatchOptions options, IReadOnlyList<SeatInfo> seats)
    {
        var playerCount = seats.Count;
        if (playerCount is not (2 or 4))
            throw new GameEngineException("Canasta requires 2 or 4 seats.");

        if (playerCount == 4 && seats.Any(s => string.IsNullOrWhiteSpace(s.TeamId)))
            throw new GameEngineException("Four-player Canasta requires team assignments.");

        var winningScore = JsonStateHelper.TryGetProperty(options.Values ?? default, "winningScore", (int?)5000) ?? 5000;
        if (winningScore < 1000)
            throw new GameEngineException("Canasta winning score must be at least 1000.");

        var teams = BuildTeams(seats);
        var data = new CanastaStateData(winningScore, teams.Select(t => new TeamScore(t.TeamId, 0)).ToArray(), []);

        return new GameState(
            GameId,
            seats,
            JsonStateHelper.SerializeToElement(new { winningScore, playerCount }),
            JsonStateHelper.SerializeToElement(data));
    }

    public GameState Apply(GameState state, ScoreEventInput input)
    {
        var data = JsonStateHelper.Deserialize<CanastaStateData>(state.Data);

        var next = input.EventType switch
        {
            SubmitRoundEvent => ApplySubmitRound(data, state.Seats, input),
            ResetEvent => ApplyReset(data, state.Seats),
            _ => throw new GameEngineException($"Unknown canasta event type '{input.EventType}'.")
        };

        return state with { Data = JsonStateHelper.SerializeToElement(next) };
    }

    public MatchStandings GetStandings(GameState state)
    {
        var data = JsonStateHelper.Deserialize<CanastaStateData>(state.Data);
        var teams = BuildTeams(state.Seats)
            .Select(team =>
            {
                var score = data.TeamScores.First(t => t.TeamId == team.TeamId).Score;
                return new TeamStanding(team.TeamId, team.DisplayName, score);
            })
            .ToArray();

        var seats = state.Seats
            .Select(seat =>
            {
                var teamId = seat.TeamId ?? seat.SeatId.ToString("N");
                var score = teams.First(t => t.TeamId == teamId).Score;
                return new SeatStanding(seat.SeatId, seat.DisplayName, teamId, score);
            })
            .ToArray();

        var winner = teams.FirstOrDefault(t => t.Score >= data.WinningScore);
        var details = JsonStateHelper.SerializeToElement(new
        {
            winningScore = data.WinningScore,
            rounds = data.Rounds,
            meldThresholds = teams.ToDictionary(t => t.TeamId, t => GetMeldThreshold(t.Score))
        });

        return new MatchStandings(seats, teams, null, winner?.TeamId, winner is not null, details);
    }

    public bool IsComplete(GameState state) => GetStandings(state).IsComplete;

    public static int GetMeldThreshold(int currentScore) =>
        currentScore switch
        {
            < 1500 => 50,
            < 3000 => 90,
            _ => 120
        };

    public static int ComputeRoundScore(RoundSideScore side) =>
        side.CardPoints +
        (side.NaturalCanastas * NaturalCanastaPoints) +
        (side.MixedCanastas * MixedCanastaPoints) +
        (side.RedThrees * RedThreePoints) +
        GoingOutBonus(side) -
        side.CountsAgainst;

    public static int GoingOutBonus(RoundSideScore side) =>
        side switch
        {
            { GoingOutBlind: true } => GoingOutBlindPoints,
            { GoingOut: true } => GoingOutPoints,
            _ => 0
        };

    private static bool IsGoingOut(RoundSideScore side) => side.GoingOut || side.GoingOutBlind;

    private static CanastaStateData ApplySubmitRound(
        CanastaStateData data,
        IReadOnlyList<SeatInfo> seats,
        ScoreEventInput input)
    {
        var payload = JsonStateHelper.Deserialize<SubmitRoundPayload>(input.Payload);
        var teams = BuildTeams(seats);
        if (payload.Sides.Count != teams.Count)
            throw new GameEngineException("Round must include a score for every team.");

        if (payload.Sides.Count(IsGoingOut) > 1)
            throw new GameEngineException("Only one side may go out in a round.");

        var teamScores = data.TeamScores.ToDictionary(t => t.TeamId, t => t.Score);
        var roundSides = new List<PersistedRoundSide>();

        foreach (var side in payload.Sides)
        {
            if (!teamScores.ContainsKey(side.TeamId))
                throw new GameEngineException($"Unknown team '{side.TeamId}'.");

            if (side.NaturalCanastas < 0 || side.MixedCanastas < 0 || side.RedThrees < 0 || side.CountsAgainst < 0)
                throw new GameEngineException("Canasta round values cannot be negative.");

            var roundScore = ComputeRoundScore(side);
            teamScores[side.TeamId] += roundScore;
            roundSides.Add(new PersistedRoundSide(
                side.TeamId,
                side.CardPoints,
                side.NaturalCanastas,
                side.MixedCanastas,
                side.RedThrees,
                IsGoingOut(side),
                side.GoingOutBlind,
                side.CountsAgainst,
                roundScore));
        }

        var round = new PersistedRound(input.EventId, roundSides, input.OccurredAtUtc);
        return data with
        {
            TeamScores = teamScores.Select(kv => new TeamScore(kv.Key, kv.Value)).ToArray(),
            Rounds = [.. data.Rounds, round]
        };
    }

    private static CanastaStateData ApplyReset(CanastaStateData data, IReadOnlyList<SeatInfo> seats)
    {
        var teams = BuildTeams(seats);
        return new CanastaStateData(
            data.WinningScore,
            teams.Select(t => new TeamScore(t.TeamId, 0)).ToArray(),
            []);
    }

    private static IReadOnlyList<TeamInfo> BuildTeams(IReadOnlyList<SeatInfo> seats)
    {
        if (seats.Count == 2)
        {
            return seats
                .Select(s => new TeamInfo(s.TeamId ?? s.SeatId.ToString("N"), s.DisplayName))
                .ToArray();
        }

        return seats
            .GroupBy(s => s.TeamId!)
            .Select(g => new TeamInfo(g.Key, string.Join(" / ", g.Select(s => s.DisplayName))))
            .ToArray();
    }

    private sealed record TeamInfo(string TeamId, string DisplayName);

    private sealed record CanastaStateData(
        int WinningScore,
        IReadOnlyList<TeamScore> TeamScores,
        IReadOnlyList<PersistedRound> Rounds);

    private sealed record TeamScore(string TeamId, int Score);

    private sealed record PersistedRound(Guid EventId, IReadOnlyList<PersistedRoundSide> Sides, DateTimeOffset OccurredAtUtc);

    private sealed record PersistedRoundSide(
        string TeamId,
        int CardPoints,
        int NaturalCanastas,
        int MixedCanastas,
        int RedThrees,
        bool GoingOut,
        bool GoingOutBlind,
        int CountsAgainst,
        int RoundScore);

    public sealed record RoundSideScore(
        string TeamId,
        int CardPoints,
        int NaturalCanastas,
        int MixedCanastas,
        int RedThrees,
        bool GoingOut,
        int CountsAgainst,
        bool GoingOutBlind = false);

    private sealed record SubmitRoundPayload(IReadOnlyList<RoundSideScore> Sides);
}
