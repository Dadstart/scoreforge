using Dadstart.Labs.ScoreForge.Api.Data;
using Dadstart.Labs.ScoreForge.Api.Repositories;
using Dadstart.Labs.ScoreForge.Api.Services;
using Dadstart.Labs.ScoreForge.Contracts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();
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

app.MapGet("/api/foundation/scoreboards", async (IScoreboardService scoreboardService, CancellationToken cancellationToken) =>
{
    var summaries = await scoreboardService
        .GetFoundationScoreboardsAsync(cancellationToken)
        .ConfigureAwait(false);

    return Results.Ok(summaries);
});

app.Run();
