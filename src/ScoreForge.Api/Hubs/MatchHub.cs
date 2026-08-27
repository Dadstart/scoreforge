using System.Security.Claims;
using Dadstart.Labs.ScoreForge.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Dadstart.Labs.ScoreForge.Api.Hubs;

[Authorize]
public sealed class MatchHub(IMatchService matchService) : Hub
{
    public static string GroupName(Guid matchId) => $"match:{matchId:D}";

    public async Task JoinMatch(Guid matchId)
    {
        var userId = GetUserId();
        if (!await matchService.CanAccessMatchAsync(matchId, userId, Context.ConnectionAborted).ConfigureAwait(false))
            throw new HubException("Match not found or access denied.");

        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(matchId), Context.ConnectionAborted).ConfigureAwait(false);
    }

    public Task LeaveMatch(Guid matchId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(matchId), Context.ConnectionAborted);

    private Guid GetUserId()
    {
        var value = Context.User?.FindFirstValue("scoreforge_user_id");
        if (value is null || !Guid.TryParse(value, out var userId))
            throw new HubException("User is not authenticated.");

        return userId;
    }
}
