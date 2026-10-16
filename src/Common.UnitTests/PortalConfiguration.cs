using Microsoft.Extensions.Configuration;

namespace Common.UnitTests;

/// <summary>
/// Builds the portal client configuration a session holds.
/// </summary>
internal static class PortalConfiguration
{
    public static IConfiguration Create(bool useSsl = true) => new ConfigurationBuilder()
        .AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ClientConfiguration:HostAddress"] = "portal.example.com",
            ["ClientConfiguration:UseSsl"] = useSsl.ToString()
        })
        .Build();
}
