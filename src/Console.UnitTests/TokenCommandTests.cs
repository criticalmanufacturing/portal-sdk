using Cmf.CustomerPortal.Sdk.Console.Commands;
using System.CommandLine;

namespace Console.UnitTests;

[CollectionDefinition("Token environment", DisableParallelization = true)]
public class TokenEnvironmentCollection;

[Collection("Token environment")]
public class TokenCommandTests(CommandLineFixture fixture) : IClassFixture<CommandLineFixture>
{
    [Theory]
    [InlineData("token")]
    [InlineData("token --verbose")]
    public void Parse_SelectsTokenCommand(string commandLine)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.Empty(result.Errors);
        Assert.IsType<TokenCommand>(result.CommandResult.Command);
    }

    [Fact]
    public void Invoke_PrintsOnlyTheEnvironmentToken()
    {
        // Arrange
        string? originalToken = Environment.GetEnvironmentVariable("CM_PORTAL_TOKEN");
        using var output = new StringWriter();
        using var error = new StringWriter();
        Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", "test-cli-token");
        try
        {
            // Act
            int exitCode = fixture.Parse("token").Invoke(new InvocationConfiguration { Output = output, Error = error });

            // Assert
            Assert.Equal(0, exitCode);
            Assert.Equal($"test-cli-token{Environment.NewLine}", output.ToString());
            Assert.Equal("", error.ToString());
        }
        finally
        {
            Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", originalToken);
        }
    }
}
