namespace Dadstart.Labs.ScoreForge.Api.Networking;

/// <summary>
/// Public hostname ports when the app listens on 80/443 but browsers reach it via NAT port mapping (e.g. 880→80, 8443→443).
/// </summary>
public sealed class NetworkingOptions
{
    public const string SectionName = "Networking";

    /// <summary>
    /// Port browsers use for HTTPS (e.g. 8443). When set, HTTP→HTTPS redirects use this port in the Location header.
    /// </summary>
    public int? PublicHttpsPort { get; set; }

    /// <summary>
    /// When true, HTTP requests are redirected to HTTPS using <see cref="PublicHttpsPort"/> when set.
    /// </summary>
    public bool RedirectHttpToHttps { get; set; }
}
