using System.Security.Claims;
using System.Text.Json;
using Dadstart.Labs.ScoreForge.Api.Auth;
using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Api.Hubs;
using Dadstart.Labs.ScoreForge.Api.Networking;
using Dadstart.Labs.ScoreForge.Api.Services;
using Dadstart.Labs.ScoreForge.Contracts;
using Dadstart.Labs.ScoreForge.Games.Abstractions;
using Dadstart.Labs.ScoreForge.Games.Canasta;
using Dadstart.Labs.ScoreForge.Games.Cribbage;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
var authOptions = new AuthProviderOptions();
builder.Configuration.GetSection(AuthProviderOptions.SectionName).Bind(authOptions);
var networking = builder.Configuration.GetSection(NetworkingOptions.SectionName).Get<NetworkingOptions>()
                 ?? new NetworkingOptions();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    // Vite proxies /api and /signin-* in Development; honor the browser-facing host for OAuth redirect URIs.
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
                               | ForwardedHeaders.XForwardedProto
                               | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddHttpsRedirection(options =>
{
    if (networking.PublicHttpsPort is > 0)
        options.HttpsPort = networking.PublicHttpsPort.Value;
});

var allowedCorsOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                         ??
                         [
                             "https://localhost:5173",
                             "http://localhost:5173",
                             "https://127.0.0.1:5173",
                             "http://127.0.0.1:5173"
                         ];

builder.Services.AddCors(options =>
{
    options.AddPolicy("ClientApp", policy =>
    {
        policy.WithOrigins(allowedCorsOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = "scoreforge.auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            return Task.CompletedTask;
        };
    });

if (authOptions.Google.IsConfigured)
{
    builder.Services.AddAuthentication().AddGoogle("Google", options =>
    {
        options.ClientId = authOptions.Google.ClientId;
        options.ClientSecret = authOptions.Google.ClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        ConfigureSpaProxiedOAuthCookies(options, builder.Environment);
        options.Events.OnCreatingTicket = context =>
        {
            context.Identity?.AddClaim(new Claim("scoreforge_provider", "Google"));
            return Task.CompletedTask;
        };
    });
}

if (authOptions.Microsoft.IsConfigured)
{
    builder.Services.AddAuthentication().AddMicrosoftAccount("Microsoft", options =>
    {
        options.ClientId = authOptions.Microsoft.ClientId;
        options.ClientSecret = authOptions.Microsoft.ClientSecret;
        options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        ConfigureSpaProxiedOAuthCookies(options, builder.Environment);
        options.Events.OnCreatingTicket = context =>
        {
            context.Identity?.AddClaim(new Claim("scoreforge_provider", "Microsoft"));
            return Task.CompletedTask;
        };
    });
}

var enableDevAuth = builder.Configuration.GetValue("Authentication:EnableDevLogin", false) ||
                    (builder.Environment.IsDevelopment() &&
                     !authOptions.Google.IsConfigured &&
                     !authOptions.Microsoft.IsConfigured);

if (enableDevAuth)
{
    builder.Services.AddAuthentication().AddScheme<AuthenticationSchemeOptions, DevAuthenticationHandler>(
        DevAuthenticationHandler.SchemeName,
        _ => { });
}

builder.Services.AddAuthorization();
builder.Services.AddSignalR();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IClaimsTransformation, ScoreForgeClaimsTransformation>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMatchService, MatchService>();
builder.Services.AddSingleton<IGameEngine, CribbageEngine>();
builder.Services.AddSingleton<IGameEngine, CanastaEngine>();

builder.Services.AddDbContext<ScoreForgeDbContext>(options =>
{
    var connectionString = builder.Configuration.GetConnectionString("ScoreForgeDb")
                           ?? "Host=localhost;Port=5432;Database=scoreforge;Username=scoreforge;Password=scoreforge";
    var provider = builder.Configuration["Database:Provider"] ?? DetectDatabaseProvider(connectionString);

    if (string.Equals(provider, "Sqlite", StringComparison.OrdinalIgnoreCase))
        options.UseSqlite(connectionString);
    else
        options.UseNpgsql(connectionString);
});

var app = builder.Build();

app.UseForwardedHeaders();

if (networking.RedirectHttpToHttps)
    app.UseHttpsRedirection();

app.UseDefaultFiles();
app.UseStaticFiles();
app.UseCors("ClientApp");
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    if (HttpMethods.IsPost(context.Request.Method) ||
        HttpMethods.IsPut(context.Request.Method) ||
        HttpMethods.IsPatch(context.Request.Method) ||
        HttpMethods.IsDelete(context.Request.Method))
    {
        if (context.Request.Path.StartsWithSegments("/api") &&
            !context.Request.Headers.ContainsKey("X-Requested-With"))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { Message = "Missing X-Requested-With header." }).ConfigureAwait(false);
            return;
        }
    }

    await next().ConfigureAwait(false);
});

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

app.MapGet("/api/auth/providers", () =>
{
    var providers = new List<AuthProviderSummary>
    {
        new("google", "Google", authOptions.Google.IsConfigured),
        new("microsoft", "Microsoft", authOptions.Microsoft.IsConfigured)
    };

    if (enableDevAuth)
        providers.Add(new AuthProviderSummary("dev", "Developer", true));

    return Results.Ok(providers);
});

app.MapGet("/api/auth/me", async (ClaimsPrincipal user, IUserService userService, CancellationToken cancellationToken) =>
{
    if (user.Identity?.IsAuthenticated != true)
        return Results.Ok(new AuthUserResponse(false, null, null, null));

    var userId = user.GetScoreForgeUserId();
    if (userId is null)
    {
        var created = await userService.UpsertFromClaimsAsync(user, cancellationToken).ConfigureAwait(false);
        userId = created.UserId;
    }

    return Results.Ok(new AuthUserResponse(
        true,
        userId,
        user.Identity.Name,
        user.FindFirstValue(ClaimTypes.Email)));
});

app.MapGet("/api/auth/login/{provider}", async (
    string provider,
    HttpContext context,
    [AsParameters] LoginQuery query) =>
{
    if (string.Equals(provider, "dev", StringComparison.OrdinalIgnoreCase) && enableDevAuth)
    {
        var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "dev-user"));
        identity.AddClaim(new Claim(ClaimTypes.Name, "Dev Player"));
        identity.AddClaim(new Claim(ClaimTypes.Email, "dev@scoreforge.local"));
        identity.AddClaim(new Claim("scoreforge_provider", "Dev"));
        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity)).ConfigureAwait(false);

        var redirectUri = string.IsNullOrWhiteSpace(query.ReturnUrl) ? "/" : query.ReturnUrl;
        return Results.Redirect(redirectUri);
    }

    var scheme = provider.ToLowerInvariant() switch
    {
        "google" when authOptions.Google.IsConfigured => "Google",
        "microsoft" when authOptions.Microsoft.IsConfigured => "Microsoft",
        _ => null
    };

    if (scheme is null)
        return Results.BadRequest(new { Message = $"Authentication provider '{provider}' is not configured." });

    var properties = new AuthenticationProperties
    {
        RedirectUri = string.IsNullOrWhiteSpace(query.ReturnUrl) ? "/" : query.ReturnUrl
    };

    await context.ChallengeAsync(scheme, properties).ConfigureAwait(false);
    return Results.Empty;
});

app.MapPost("/api/auth/logout", async (HttpContext context) =>
{
    await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme).ConfigureAwait(false);
    return Results.NoContent();
});

app.MapGet("/api/games", async (IMatchService matchService, CancellationToken cancellationToken) =>
    Results.Ok(await matchService.GetGamesAsync(cancellationToken).ConfigureAwait(false)))
    .RequireAuthorization();

app.MapGet("/api/matches", async (ClaimsPrincipal user, IMatchService matchService, CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    return Results.Ok(await matchService.ListMatchesAsync(userId.Value, cancellationToken).ConfigureAwait(false));
}).RequireAuthorization();

app.MapPost("/api/matches", async (
    ClaimsPrincipal user,
    CreateMatchRequest request,
    IMatchService matchService,
    CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    try
    {
        var snapshot = await matchService.CreateMatchAsync(userId.Value, request, cancellationToken).ConfigureAwait(false);
        return Results.Created($"/api/matches/{snapshot.MatchId}", snapshot);
    }
    catch (Exception ex) when (ex is InvalidOperationException or GameEngineException)
    {
        return Results.BadRequest(new { Message = ex.Message });
    }
}).RequireAuthorization();

app.MapGet("/api/matches/{matchId:guid}", async (
    Guid matchId,
    ClaimsPrincipal user,
    IMatchService matchService,
    CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    var snapshot = await matchService.GetMatchAsync(matchId, userId.Value, cancellationToken).ConfigureAwait(false);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
}).RequireAuthorization();

app.MapPost("/api/matches/{matchId:guid}/seats/{seatId:guid}/claim", async (
    Guid matchId,
    Guid seatId,
    ClaimsPrincipal user,
    IMatchService matchService,
    CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    try
    {
        var snapshot = await matchService.ClaimSeatAsync(matchId, seatId, userId.Value, cancellationToken).ConfigureAwait(false);
        return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
    }
    catch (InvalidOperationException ex)
    {
        return Results.Conflict(new { Message = ex.Message });
    }
}).RequireAuthorization();

app.MapPost("/api/matches/{matchId:guid}/events", async (
    Guid matchId,
    ClaimsPrincipal user,
    AppendMatchEventRequest request,
    IMatchService matchService,
    CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    var result = await matchService.AppendEventAsync(matchId, userId.Value, request, cancellationToken).ConfigureAwait(false);
    return ToAppendResult(result);
}).RequireAuthorization();

app.MapPost("/api/matches/{matchId:guid}/events/undo", async (
    Guid matchId,
    ClaimsPrincipal user,
    UndoMatchEventRequest request,
    IMatchService matchService,
    CancellationToken cancellationToken) =>
{
    var userId = user.GetScoreForgeUserId();
    if (userId is null)
        return Results.Unauthorized();

    var result = await matchService.UndoEventAsync(matchId, userId.Value, request, cancellationToken).ConfigureAwait(false);
    return ToAppendResult(result);
}).RequireAuthorization();

app.MapHub<MatchHub>("/hubs/match");

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.MapFallbackToFile("index.html");
app.Run();

static void ConfigureSpaProxiedOAuthCookies(RemoteAuthenticationOptions options, IHostEnvironment environment)
{
    if (!environment.IsDevelopment())
        return;

    // Default OAuth correlation cookies are SameSite=None + Secure, which browsers drop on the
    // HTTP Vite origin. Lax works for the top-level OAuth callback navigation.
    options.CorrelationCookie.SameSite = SameSiteMode.Lax;
    options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
}

static IResult ToAppendResult(MatchAppendResult result) =>
    result.StatusCode switch
    {
        StatusCodes.Status200OK => Results.Ok(result.Snapshot),
        StatusCodes.Status409Conflict => Results.Json(result.Snapshot, statusCode: StatusCodes.Status409Conflict),
        StatusCodes.Status404NotFound => Results.NotFound(new { Message = result.Error }),
        StatusCodes.Status400BadRequest => Results.BadRequest(new { Message = result.Error }),
        _ => Results.Json(new { Message = result.Error }, statusCode: result.StatusCode ?? 500)
    };

static string DetectDatabaseProvider(string connectionString) =>
    connectionString.Contains("Data Source=", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("Filename=", StringComparison.OrdinalIgnoreCase)
        ? "Sqlite"
        : "Npgsql";

public sealed record LoginQuery(string? ReturnUrl);

public partial class Program;
