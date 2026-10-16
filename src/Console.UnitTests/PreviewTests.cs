using Cmf.CustomerPortal.Sdk.Console;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Preview commands flag it in their help description and warn about it when run.
/// </summary>
public class PreviewTests
{
    [Theory]
    [InlineData("get -h")]
    [InlineData("get env -h")]
    [InlineData("get deploymentpackage -h")]
    [InlineData("logs -h")]
    [InlineData("logs env -h")]
    public void PreviewCommandHelp_SaysItIsAPreview(string commandLine)
    {
        // Act
        string help = CommandLineFixture.Help(commandLine);

        // Assert
        Assert.Contains("[Preview]", help);
    }

    [Fact]
    public void RootHelp_FlagsOnlyThePreviewVerbs()
    {
        // Act
        string help = CommandLineFixture.Help("-h");

        // Assert
        string[] previewLines = help.Split('\n').Where(line => line.Contains("[Preview]")).Select(line => line.Trim()).ToArray();
        Assert.Equal(2, previewLines.Length);
        Assert.StartsWith("get ", previewLines[0]);
        Assert.StartsWith("logs ", previewLines[1]);
    }

    [Theory]
    [InlineData("deploy env -h")]
    [InlineData("healthcheck agent -h")]
    [InlineData("download artifacts -h")]
    public void OtherCommandHelp_IsNotAPreview(string commandLine)
    {
        // Act
        string help = CommandLineFixture.Help(commandLine);

        // Assert
        Assert.DoesNotContain("[Preview]", help);
    }

    [Theory]
    [InlineData("get", "env")]
    [InlineData("get", "deploymentpackage")]
    [InlineData("logs", "env")]
    public void PreviewCommand_WhenRun_WarnsThatItIsAPreview(string verb, string noun)
    {
        // Arrange
        Command verbCommand = RootCommandFactory.Create().Subcommands.Single(c => c.Name == verb);
        var command = (BaseCommand)verbCommand.Subcommands.Single(c => c.Name == noun);
        using var error = new StringWriter();

        // Act
        command.WarnIfPreview(error);

        // Assert
        Assert.Equal($"[Preview] '{verb} {noun}' is a preview: its options and output may change in a future version.", error.ToString().Trim());
    }

    [Fact]
    public void OtherCommand_WhenRun_DoesNotWarn()
    {
        // Arrange
        Command deploy = RootCommandFactory.Create().Subcommands.Single(c => c.Name == "deploy");
        var command = (BaseCommand)deploy.Subcommands.Single(c => c.Name == "env");
        using var error = new StringWriter();

        // Act
        command.WarnIfPreview(error);

        // Assert
        Assert.Empty(error.ToString());
    }
}
