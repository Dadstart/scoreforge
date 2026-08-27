using System.Security.Claims;
using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Dadstart.Labs.ScoreForge.Api.Services;

public interface IUserService
{
    Task<UserEntity> UpsertFromClaimsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken);
    Task<UserEntity?> FindByIdAsync(Guid userId, CancellationToken cancellationToken);
}

public sealed class UserService(ScoreForgeDbContext dbContext) : IUserService
{
    public async Task<UserEntity> UpsertFromClaimsAsync(ClaimsPrincipal principal, CancellationToken cancellationToken)
    {
        var provider = principal.FindFirstValue("scoreforge_provider")
                       ?? principal.Identity?.AuthenticationType
                       ?? "unknown";
        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                      ?? throw new InvalidOperationException("Authenticated user is missing a subject claim.");
        var displayName = principal.Identity?.Name
                          ?? principal.FindFirstValue(ClaimTypes.Email)
                          ?? "Player";
        var email = principal.FindFirstValue(ClaimTypes.Email);
        var now = DateTimeOffset.UtcNow;

        var user = await dbContext.Users
            .FirstOrDefaultAsync(u => u.Provider == provider && u.ProviderSubject == subject, cancellationToken)
            .ConfigureAwait(false);

        if (user is null)
        {
            user = new UserEntity
            {
                UserId = Guid.NewGuid(),
                Provider = provider,
                ProviderSubject = subject,
                DisplayName = displayName,
                Email = email,
                CreatedAtUtc = now,
                LastLoginAtUtc = now
            };
            dbContext.Users.Add(user);
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return user;
        }

        var changed = false;
        if (!string.Equals(user.DisplayName, displayName, StringComparison.Ordinal))
        {
            user.DisplayName = displayName;
            changed = true;
        }

        if (!string.Equals(user.Email, email, StringComparison.Ordinal))
        {
            user.Email = email;
            changed = true;
        }

        if (changed)
        {
            user.LastLoginAtUtc = now;
            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return user;
    }

    public Task<UserEntity?> FindByIdAsync(Guid userId, CancellationToken cancellationToken) =>
        dbContext.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
}
