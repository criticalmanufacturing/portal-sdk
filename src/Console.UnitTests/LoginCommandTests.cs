using System.Text.Json;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands;

namespace Console.UnitTests;

/// <summary>
/// The <c>login</c> command and its sign-in through a redirect URI.
/// </summary>
public class LoginCommandTests(CommandLineFixture fixture) : IClassFixture<CommandLineFixture>
{
    private const string SignInUrl = "https://security.example.com/login?a=1&b=2";

    [Theory]
    [InlineData("login")]
    [InlineData("login --token pat")]
    [InlineData("login --redirect-uri https://service.example.com/callback")]
    [InlineData("login --redirect-uri https://service.example.com:8443/callback -v")]
    public void Login_ParsesWithoutErrors(string commandLine)
    {
        // Act & Assert
        Assert.Empty(fixture.Parse(commandLine).Errors);
    }

    [Theory]
    [InlineData("login --redirect-uri http://service.example.com/callback", Resources.LoginRedirectUriInvalidMessage)]
    [InlineData("login --redirect-uri /callback", Resources.LoginRedirectUriInvalidMessage)]
    [InlineData(
        "login --redirect-uri https://service.example.com/callback --token pat",
        Resources.LoginRedirectUriWithTokenMessage
    )]
    public void Login_WithInvalidRedirectUriOptions_IsRejected(string commandLine, string expectedError)
    {
        // Act & Assert
        Assert.Equal([expectedError], fixture.Parse(commandLine).Errors.Select(e => e.Message));
    }

    [Fact]
    public async Task WaitForCallback_WritesTheSignInUrlAsJsonAndReturnsTheCallbackLine()
    {
        // Arrange
        using var output = new StringWriter();
        using var input = new StringReader("https://service.example.com/callback?code=c&state=s\nignored\n");

        // Act
        string callback = await LoginCommand.WaitForCallback(output, input, new Uri(SignInUrl));

        // Assert
        Assert.Equal("https://service.example.com/callback?code=c&state=s", callback);
        using JsonDocument json = JsonDocument.Parse(output.ToString());
        Assert.Equal(SignInUrl, json.RootElement.GetProperty("authorizationUrl").GetString());
    }

    [Fact]
    public async Task WaitForCallback_WhenTheInputIsClosed_ReturnsNull()
    {
        // Act
        string callback = await LoginCommand.WaitForCallback(TextWriter.Null, new StringReader(""), new Uri(SignInUrl));

        // Assert
        Assert.Null(callback);
    }

    [Fact]
    public void WriteRefreshToken_WritesTheTokenAsJson()
    {
        // Arrange
        using var output = new StringWriter();

        // Act
        LoginCommand.WriteRefreshToken(output, "refresh-token");

        // Assert
        using JsonDocument json = JsonDocument.Parse(output.ToString());
        Assert.Equal("refresh-token", json.RootElement.GetProperty("refreshToken").GetString());
    }

    [Fact]
    public void LoginHelp_FlagsTheRedirectUriOptionAsAPreview()
    {
        // Act
        string help = CommandLineFixture.Help("login -h");

        // Assert
        Assert.Contains(help.Split('\n'), line => line.Contains("--redirect-uri") && line.Contains("[Preview]"));
    }
}
