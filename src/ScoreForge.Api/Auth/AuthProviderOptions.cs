namespace Dadstart.Labs.ScoreForge.Api.Auth;

public sealed class AuthProviderOptions
{
    public const string SectionName = "Authentication";

    public OAuthProviderOptions Google { get; init; } = new();
    public OAuthProviderOptions Microsoft { get; init; } = new();
}

public sealed class OAuthProviderOptions
{
    public string ClientId { get; init; } = string.Empty;
    public string ClientSecret { get; init; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);
}
