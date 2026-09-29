using Cmf.CustomerPortal.Sdk.Console;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Legacy commands flag their deprecation in their help description and warn about it when run.
/// </summary>
public class DeprecationTests
{
    [Theory]
    [InlineData("checkagentconnection", "healthcheck agent")]
    [InlineData("createinfrastructure", "create infrastructure")]
    [InlineData("deployagent", "deploy agent")]
    [InlineData("download-artifacts", "download artifacts")]
    [InlineData("install-app", "deploy app")]
    [InlineData("publish-package", "publish installationpackage")]
    [InlineData("uninstall-app", "undeploy app")]
    public void LegacyCommandHelp_SaysItIsDeprecatedAndNamesItsReplacement(string commandName, string replacement)
    {
        // Act
        string help = CommandLineFixture.Help($"{commandName} -h");

        // Assert
        Assert.Contains("[Deprecated]", help);
        Assert.Contains($"Use '{replacement}' instead.", help);
    }

    [Theory]
    [InlineData("login -h")]
    [InlineData("deploy -h")]
    [InlineData("deploy env -h")]
    [InlineData("undeploy app -h")]
    [InlineData("healthcheck agent -h")]
    public void NewCommandHelp_IsNotDeprecated(string commandLine)
    {
        // Act
        string help = CommandLineFixture.Help(commandLine);

        // Assert
        Assert.DoesNotContain("[Deprecated]", help);
    }

    [Fact]
    public void RootHelp_ListsEachDeprecatedCommandWithItsReplacement()
    {
        // Act
        string help = CommandLineFixture.Help("-h");

        // Assert
        Assert.Equal(7, help.Split('\n').Count(line => line.Contains("[Deprecated]") && line.Contains("Use '")));
    }

    [Theory]
    [InlineData("checkagentconnection", "healthcheck agent")]
    [InlineData("createinfrastructure", "create infrastructure")]
    [InlineData("deploy", "deploy env")]
    [InlineData("deployagent", "deploy agent")]
    [InlineData("download-artifacts", "download artifacts")]
    [InlineData("install-app", "deploy app")]
    [InlineData("publish", "publish deploymentpackage")]
    [InlineData("publish-package", "publish installationpackage")]
    [InlineData("undeploy", "undeploy env")]
    [InlineData("uninstall-app", "undeploy app")]
    public void LegacyCommand_WhenRun_WarnsThatItIsDeprecatedAndNamesItsReplacement(string commandName, string replacement)
    {
        // Arrange
        var command = (BaseCommand)RootCommandFactory.Create().Subcommands.Single(c => c.Name == commandName);
        using var error = new StringWriter();

        // Act
        command.WarnIfDeprecated(error);

        // Assert
        string warning = error.ToString().Trim();
        Assert.StartsWith($"[Deprecated] '{commandName}' ", warning);
        Assert.EndsWith($"Use '{replacement}' instead.", warning);
    }

    [Theory]
    [InlineData("login")]
    [InlineData("deploy env")]
    [InlineData("deploy agent")]
    [InlineData("deploy app")]
    [InlineData("undeploy env")]
    [InlineData("undeploy app")]
    [InlineData("publish deploymentpackage")]
    [InlineData("publish installationpackage")]
    [InlineData("create infrastructure")]
    [InlineData("healthcheck agent")]
    [InlineData("download artifacts")]
    public void NewCommand_WhenRun_DoesNotWarn(string commandLine)
    {
        // Arrange
        var command = (BaseCommand)RootCommandFactory.Create().Parse(commandLine).CommandResult.Command;
        using var error = new StringWriter();

        // Act
        command.WarnIfDeprecated(error);

        // Assert
        Assert.Null(command.DeprecationMessage);
        Assert.Empty(error.ToString());
    }
}