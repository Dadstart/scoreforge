using System.Text;
using Dadstart.Labs.ScoreForge.Api.Networking;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Dadstart.Labs.ScoreForge.Api.Tests;

public sealed class NetworkingOptionsTests
{
    [Fact]
    public void NetworkingOptions_Binds_FromJson()
    {
        const string json = """
            {
              "Networking": {
                "PublicHttpsPort": 8443,
                "RedirectHttpToHttps": true
              }
            }
            """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        var configuration = new ConfigurationBuilder()
            .AddJsonStream(stream)
            .Build();

        var options = configuration.GetSection(NetworkingOptions.SectionName).Get<NetworkingOptions>();

        Assert.NotNull(options);
        Assert.Equal(8443, options.PublicHttpsPort);
        Assert.True(options.RedirectHttpToHttps);
    }
}
