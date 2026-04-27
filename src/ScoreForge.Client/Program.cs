using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Dadstart.Labs.ScoreForge.Client;
using Dadstart.Labs.ScoreForge.Client.Services;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

var apiBaseUrl = builder.Configuration["ApiBaseUrl"];
var apiBaseAddress = Uri.TryCreate(apiBaseUrl, UriKind.Absolute, out var configuredApiUri)
    ? configuredApiUri
    : new Uri(builder.HostEnvironment.BaseAddress);

builder.Services.AddScoped(_ => new HttpClient { BaseAddress = apiBaseAddress });
builder.Services.AddScoped<ILocalStore, IndexedDbLocalStore>();
builder.Services.AddScoped<AuthApiClient>();
builder.Services.AddScoped<ApiAuthenticationStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<ApiAuthenticationStateProvider>());
builder.Services.AddAuthorizationCore();

await builder.Build().RunAsync();
