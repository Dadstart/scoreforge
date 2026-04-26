using System.Collections.Concurrent;

namespace Dadstart.Labs.ScoreForge.Client.Services;

public sealed class InMemoryLocalStore : ILocalStore
{
    private readonly ConcurrentDictionary<string, object?> _cache = new();

    public Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        _cache[key] = value;
        return Task.CompletedTask;
    }

    public Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (!_cache.TryGetValue(key, out var value))
            return Task.FromResult(default(T));

        return value is T typedValue
            ? Task.FromResult<T?>(typedValue)
            : Task.FromResult(default(T));
    }
}
