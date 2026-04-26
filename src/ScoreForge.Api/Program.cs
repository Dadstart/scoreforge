using Dadstart.Labs.ScoreForge.Contracts;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseHttpsRedirection();

var sample = new ScoreboardSummary(
    Guid.Parse("A09B8E7B-7428-4577-B1DF-4A7E9AA16E59"),
    "Starter Match",
    0,
    DateTimeOffset.UtcNow);

app.MapGet("/api/health", () => Results.Ok(new
{
    Status = "ok",
    Service = "ScoreForge.Api",
    UtcNow = DateTimeOffset.UtcNow
}));

app.MapGet("/api/foundation/scoreboards", () => Results.Ok(new[] { sample }));

app.Run();
