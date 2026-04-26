using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace Dadstart.Labs.ScoreForge.Client.Services;

public sealed class ApiAuthenticationStateProvider(AuthApiClient authApiClient) : AuthenticationStateProvider
{
    private static readonly AuthenticationState AnonymousState =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var user = await authApiClient.GetCurrentUserAsync();
            if (!user.IsAuthenticated)
                return AnonymousState;

            var claims = new List<Claim>();
            if (!string.IsNullOrWhiteSpace(user.DisplayName))
                claims.Add(new Claim(ClaimTypes.Name, user.DisplayName));
            if (!string.IsNullOrWhiteSpace(user.Email))
                claims.Add(new Claim(ClaimTypes.Email, user.Email));

            var identity = new ClaimsIdentity(claims, "ScoreForgeCookie");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return AnonymousState;
        }
    }

    public void NotifyAuthenticationStateChanged() =>
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
