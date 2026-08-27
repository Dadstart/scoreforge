using System.Net.Http.Json;
using Dadstart.Labs.ScoreForge.Contracts;
using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace Dadstart.Labs.ScoreForge.Client.Services;

public sealed class AuthApiClient(HttpClient httpClient)
{
    public async Task<AuthUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<AuthUserResponse>(cancellationToken);
        return payload ?? new AuthUserResponse(false, null, null);
    }

    public async Task<IReadOnlyList<AuthProviderSummary>> GetProvidersAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/providers");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<List<AuthProviderSummary>>(cancellationToken);
        return payload ?? [];
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/auth/logout");
        request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }
}
