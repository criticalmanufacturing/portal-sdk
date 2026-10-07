using Cmf.CustomerPortal.Sdk.Console;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Help lists the verbs and the deprecated legacy commands, and every command documents itself.
/// </summary>
public class HelpTests
{
    [Fact]
    public void RootHelp_ListsVerbsFollowedByDeprecatedLegacyCommands()
    {
        // Act
        string help = CommandLineFixture.Help("-h");

        // Assert
        Assert.Equal(
            ["login", "deploy", "undeploy", "publish", "create", "healthcheck", "download",
             "checkagentconnection", "createinfrastructure", "deployagent", "download-artifacts", "install-app", "publish-package", "uninstall-app"],
            ListedCommands(help));
    }

    [Theory]
    [InlineData("deploy -h", new[] { "agent", "app, application", "env, environment" })]
    [InlineData("undeploy -h", new[] { "app, application", "env, environment" })]
    [InlineData("publish -h", new[] { "deploymentpackage", "installationpackage" })]
    [InlineData("create -h", new[] { "infra, infrastructure" })]
    [InlineData("healthcheck -h", new[] { "agent" })]
    [InlineData("download -h", new[] { "artifacts" })]
    public void VerbHelp_ListsNouns(string commandLine, string[] expectedNouns)
    {
        // Act
        string help = CommandLineFixture.Help(commandLine);

        // Assert
        Assert.Equal(expectedNouns, ListedCommands(help).Order());
    }

    [Theory]
    [InlineData("checkagentconnection", "--agent-name")]
    [InlineData("createinfrastructure", "--customer")]
    [InlineData("deployagent", "--terminateOtherVersions")]
    [InlineData("download-artifacts", "--name")]
    [InlineData("install-app", "--app-version")]
    [InlineData("publish-package", "--path")]
    [InlineData("uninstall-app", "--removeVolumes")]
    public void LegacyCommandHelp_ListsItsOptions(string commandName, string expectedOption)
    {
        // Act
        string help = CommandLineFixture.Help($"{commandName} -h");

        // Assert
        Assert.Contains($" {commandName} [options]", help);
        Assert.Contains(expectedOption, help);
    }

    [Fact]
    public void DeployHelp_DoesNotListLegacyOptions()
    {
        // Act
        string help = CommandLineFixture.Help("deploy -h");

        // Assert
        Assert.DoesNotContain("--verbose", help);
        Assert.DoesNotContain("--license", help);
    }

    [Fact]
    public void EveryCommandAndArgument_HasADescription()
    {
        // Arrange
        RootCommand root = RootCommandFactory.Create();

        // Act
        var commands = root.Subcommands
            .SelectMany(verb => verb.Subcommands.Prepend(verb))
            .ToList();

        // Assert
        Assert.All(commands, c => Assert.False(string.IsNullOrWhiteSpace(c.Description), $"{c.Name} has no description"));
        Assert.All(commands.SelectMany(c => c.Arguments), a => Assert.False(string.IsNullOrWhiteSpace(a.Description), $"{a.Name} has no description"));
    }

    private static IEnumerable<string> ListedCommands(string help)
    {
        // names and aliases of each line in the "Commands:" section, e.g. "env, environment <name>  ..." -> "env, environment"
        return help.Split('\n')
            .SkipWhile(line => !line.StartsWith("Commands:"))
            .Skip(1)
            .TakeWhile(line => line.StartsWith("  "))
            .Select(line => line.Trim().Split("  ")[0].Split(" <")[0]);
    }
}
