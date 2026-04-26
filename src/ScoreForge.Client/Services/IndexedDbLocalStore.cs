using System.Text.Json;
using Microsoft.JSInterop;

namespace Dadstart.Labs.ScoreForge.Client.Services;

public sealed class IndexedDbLocalStore(IJSRuntime jsRuntime) : ILocalStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = JsonSerializer.Serialize(value, JsonOptions);

        await jsRuntime.InvokeVoidAsync(
            "scoreForgeLocalStore.set",
            cancellationToken,
            key,
            payload);
    }

    public async Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var payload = await jsRuntime.InvokeAsync<string?>(
            "scoreForgeLocalStore.get",
            cancellationToken,
            key);

        if (string.IsNullOrWhiteSpace(payload))
            return default;

        return JsonSerializer.Deserialize<T>(payload, JsonOptions);
    }
}
