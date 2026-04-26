using System.Net.Http.Json;
using Dadstart.Labs.ScoreForge.Contracts;

namespace Dadstart.Labs.ScoreForge.Client.Services;

public sealed class AuthApiClient(HttpClient httpClient)
{
    public async Task<AuthUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<AuthUserResponse>("/api/auth/me", cancellationToken);
        return response ?? new AuthUserResponse(false, null, null);
    }

    public async Task<IReadOnlyList<AuthProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        var response = await httpClient.GetFromJsonAsync<List<AuthProviderSummary>>("/api/auth/providers", cancellationToken);
        return response ?? [];
    }
}
