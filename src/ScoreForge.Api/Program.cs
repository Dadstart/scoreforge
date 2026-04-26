using System.Security.Claims;
using Dadstart.Labs.ScoreForge.Api.Auth;
using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Api.Repositories;
using Dadstart.Labs.ScoreForge.Api.Services;
using Dadstart.Labs.ScoreForge.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var authOptions = new AuthProviderOptions();
builder.Configuration.GetSection(AuthProviderOptions.SectionName).Bind(authOptions);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "scoreforge.auth";
        options.LoginPath = "/api/auth/login/google";
        options.LogoutPath = "/api/auth/logout";
    });

if (authOptions.Google.IsConfigured)
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.ClientId = authOptions.Google.ClientId;
        options.ClientSecret = authOptions.Google.ClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });
}

if (authOptions.Microsoft.IsConfigured)
{
    builder.Services.AddAuthentication().AddMicrosoftAccount("Microsoft", options =>
    {
        options.ClientId = authOptions.Microsoft.ClientId;
        options.ClientSecret = authOptions.Microsoft.ClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    });
}

builder.Services.AddAuthorization();
builder.Services.AddDbContext<ScoreForgeDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ScoreForgeDb")
                           ?? "Data Source=scoreforge.db";

    options.UseSqlite(connectionString);
});
builder.Services.AddScoped<IScoreboardRepository, EfScoreboardRepository>();
builder.Services.AddScoped<IScoreboardService, ScoreboardService>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ScoreForgeDbContext>();
    await ScoreForgeDbInitializer.InitializeAsync(dbContext).ConfigureAwait(false);
}

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "ok",
    Service = "ScoreForge.Api",
    UtcNow = DateTimeOffset.UtcNow
}));

app.MapGet("/api/auth/providers", () => Results.Ok(new[]
{
    new AuthProviderSummary("google", "Google", authOptions.Google.IsConfigured),
    new AuthProviderSummary("microsoft", "Microsoft", authOptions.Microsoft.IsConfigured)
}));

app.MapGet("/api/auth/me", (ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
        return Results.Ok(new AuthUserResponse(false, null, null));

    return Results.Ok(new AuthUserResponse(
        true,
        user.Identity.Name,
        user.FindFirstValue(ClaimTypes.Email)));
});

app.MapGet("/api/auth/login/{provider}", async (
    string provider,
    HttpContext context,
    [AsParameters] LoginQuery query) =>
{
    var scheme = provider.ToLowerInvariant() switch
    {
        "google" when authOptions.Google.IsConfigured => "Google",
        "microsoft" when authOptions.Microsoft.IsConfigured => "Microsoft",
        _ => null
    };

    if (scheme is null)
        return Results.BadRequest(new
        {
            Message = $"Authentication provider '{provider}' is not configured."
        });

    var redirectUri = string.IsNullOrWhiteSpace(query.ReturnUrl) ? "/" : query.ReturnUrl;
    var properties = new AuthenticationProperties
    {
        RedirectUri = redirectUri
    };

    await context.ChallengeAsync(scheme, properties).ConfigureAwait(false);
    return Results.Empty;
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    return Results.NoContent();
});

app.MapGet("/api/foundation/scoreboards", async (IScoreboardService scoreboardService, CancellationToken cancellationToken) =>
{
    var summaries = await scoreboardService
        .GetFoundationScoreboardsAsync(cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(summaries);
}).RequireAuthorization();

app.Run();

public sealed record LoginQuery(string? ReturnUrl);
