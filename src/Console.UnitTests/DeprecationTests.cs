using Cmf.CustomerPortal.Sdk.Console;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// Legacy commands flag their deprecation in their help description.
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
}
