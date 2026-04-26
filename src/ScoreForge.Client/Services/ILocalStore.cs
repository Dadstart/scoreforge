namespace Dadstart.Labs.ScoreForge.Client.Services;

public interface ILocalStore
{
    Task SetItemAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task<T?> GetItemAsync<T>(string key, CancellationToken cancellationToken = default);
}
