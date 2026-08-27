using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Dadstart.Labs.ScoreForge.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Dadstart.Labs.ScoreForge.Api.Tests;

public sealed class MatchApiTests : IClassFixture<MatchApiTests.ScoreForgeApiFactory>
{
    private readonly ScoreForgeApiFactory _factory;

    public MatchApiTests(ScoreForgeApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/api/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DevLogin_CreateMatch_AppendEvent_IsIdempotent()
    {
        var client = await CreateAuthenticatedClientAsync();

        var me = await client.GetFromJsonAsync<AuthUserResponse>("/api/auth/me");
        Assert.NotNull(me);
        Assert.True(me.IsAuthenticated);

        using var createRequest = CreatePost("/api/matches", new CreateMatchRequest(
            "Test Cribbage",
            "cribbage",
            JsonSerializer.SerializeToElement(new { winningScore = 121 }),
            [
                new CreateMatchSeatRequest("Alice", null),
                new CreateMatchSeatRequest("Bob", null)
            ]));
        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var snapshot = await createResponse.Content.ReadFromJsonAsync<MatchSnapshotDto>();
        Assert.NotNull(snapshot);

        var seatId = snapshot.Seats[0].SeatId;
        var clientEventId = "evt-idempotent-1";
        var appendBody = new AppendMatchEventRequest(
            "add_points",
            JsonSerializer.SerializeToElement(new { seatId, points = 5 }),
            snapshot.Version,
            clientEventId);

        using var append1 = CreatePost($"/api/matches/{snapshot.MatchId}/events", appendBody);
        var response1 = await client.SendAsync(append1);
        var body1 = await response1.Content.ReadAsStringAsync();
        Assert.True(response1.IsSuccessStatusCode, $"Append failed ({(int)response1.StatusCode}): {body1}");
        var after1 = await response1.Content.ReadFromJsonAsync<MatchSnapshotDto>();
        Assert.NotNull(after1);
        Assert.Equal(5, after1.Standings.Seats[0].Score);

        using var append2 = CreatePost($"/api/matches/{snapshot.MatchId}/events", appendBody);
        var response2 = await client.SendAsync(append2);
        Assert.Equal(HttpStatusCode.OK, response2.StatusCode);
        var after2 = await response2.Content.ReadFromJsonAsync<MatchSnapshotDto>();
        Assert.NotNull(after2);
        Assert.Equal(after1.Version, after2.Version);
        Assert.Equal(5, after2.Standings.Seats[0].Score);
    }

    [Fact]
    public async Task AppendEvent_WithStaleVersion_ReturnsConflict()
    {
        var client = await CreateAuthenticatedClientAsync();

        using var createRequest = CreatePost("/api/matches", new CreateMatchRequest(
            "Conflict Match",
            "cribbage",
            JsonSerializer.SerializeToElement(new { winningScore = 121 }),
            [
                new CreateMatchSeatRequest("A", null),
                new CreateMatchSeatRequest("B", null)
            ]));
        var createResponse = await client.SendAsync(createRequest);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var created = await createResponse.Content.ReadFromJsonAsync<MatchSnapshotDto>();
        Assert.NotNull(created);

        using var stale = CreatePost($"/api/matches/{created.MatchId}/events", new AppendMatchEventRequest(
            "add_points",
            JsonSerializer.SerializeToElement(new { seatId = created.Seats[0].SeatId, points = 2 }),
            BaseVersion: 999,
            ClientEventId: "stale-1"));
        var response = await client.SendAsync(stale);
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

        var login = await client.GetAsync("/api/auth/login/dev?returnUrl=/");
        Assert.True(
            login.StatusCode is HttpStatusCode.Redirect or HttpStatusCode.Found or HttpStatusCode.OK,
            $"Unexpected login status: {(int)login.StatusCode}");
        return client;
    }

    private static HttpRequestMessage CreatePost<T>(string url, T body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Add("X-Requested-With", "XMLHttpRequest");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return request;
    }

    public sealed class ScoreForgeApiFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"scoreforge-tests-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");
            builder.UseSetting("Database:Provider", "Sqlite");
            builder.UseSetting("ConnectionStrings:ScoreForgeDb", $"Data Source={_dbPath}");
            builder.UseSetting("Authentication:EnableDevLogin", "true");
            builder.UseSetting("Authentication:Google:ClientId", "");
            builder.UseSetting("Authentication:Google:ClientSecret", "");
            builder.UseSetting("Authentication:Microsoft:ClientId", "");
            builder.UseSetting("Authentication:Microsoft:ClientSecret", "");
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing)
                return;

            try
            {
                if (File.Exists(_dbPath))
                    File.Delete(_dbPath);
            }
            catch
            {
                // ignore cleanup failures
            }
        }
    }
}
