using System.Security.Claims;
using Dadstart.Labs.ScoreForge.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Dadstart.Labs.ScoreForge.Api.Auth;

public sealed class ScoreForgeClaimsTransformation(IUserService userService) : IClaimsTransformation
{
    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        if (principal.HasClaim(c => c.Type == "scoreforge_user_id"))
            return principal;

        var user = await userService.UpsertFromClaimsAsync(principal, CancellationToken.None).ConfigureAwait(false);
        var identity = principal.Identities.FirstOrDefault(i => i.IsAuthenticated)
                       ?? new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);

        identity.AddClaim(new Claim("scoreforge_user_id", user.UserId.ToString("D")));
        return principal;
    }
}

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetScoreForgeUserId(this ClaimsPrincipal principal)
    {
        var value = principal.FindFirstValue("scoreforge_user_id");
        return Guid.TryParse(value, out var userId) ? userId : null;
    }
}
