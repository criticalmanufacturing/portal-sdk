using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Arguments that start with <c>@</c> are read as response files only when the file exists.
/// </summary>
public class ResponseFileTests(CommandLineFixture fixture) : IClassFixture<CommandLineFixture>
{
    [Fact]
    public void ExistingResponseFile_IsExpanded()
    {
        // Arrange
        string responseFile = Path.Combine(fixture.ExistingDirectory, "get-env.rsp");
        File.WriteAllLines(responseFile, ["env", "my-env", "--version", "3"]);

        // Act
        ParseResult result = fixture.Parse($"get \"@{responseFile}\"");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal("my-env", result.GetValue<string>("name"));
        Assert.Equal(3, result.GetValue<int?>("--version"));
    }

    [Fact]
    public void ArgumentThatIsNotAFile_IsKeptAsItIs()
    {
        // Act
        ParseResult result = fixture.Parse("get deploymentpackage @criticalmanufacturing\\missing:1.0.0");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal("@criticalmanufacturing\\missing:1.0.0", result.GetValue<string>("name"));
    }
}
