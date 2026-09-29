using Cmf.CustomerPortal.Sdk.Console;
using Cmf.CustomerPortal.Sdk.Console.Commands;
using Cmf.CustomerPortal.Sdk.Console.Commands.Legacy;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// The legacy flat commands must keep accepting exactly what they accepted before the verb-noun commands existed.
/// </summary>
public class LegacyCommandTests(CommandLineFixture fixture) : IClassFixture<CommandLineFixture>
{
    /// <summary>
    /// Option aliases and required flags per legacy command, as printed by <c>-h</c> before the change.
    /// </summary>
    public static TheoryData<string, string[]> LegacyOptions => new()
    {
        { "checkagentconnection", ["--verbose -v", "--agent-name --name -n", "--customer-environment -ce"] },
        { "createinfrastructure", ["--verbose -v", "--name -n", "--site -s", "--customer -c", "--ignore-if-exists", "--parameters -params"] },
        { "deployagent", ["--verbose -v", "--replace-tokens", "--customer-infrastructure-name -ci", "--name -n", "--alias -a", "--description -d",
            "--parameters -params", "--type -type", "--target -trg", "--output -o", "--interactive -i", "--terminateOtherVersions -tov",
            "--deploymentTimeoutMinutes -to", "--deploymentTimeoutMinutesToGetSomeMBMsg -tombm", "--terminateOtherVersionsRemove -tovr",
            "--terminateOtherVersionsRemoveVolumes -tovrv"] },
        { "deploy", ["--verbose -v", "--replace-tokens", "--customer-infrastructure-name -ci", "--name -n", "--alias -a", "--description -d",
            "--parameters -params", "--type -type", "--target -trg", "--output -o", "--interactive -i", "--site -s", "--package -pck",
            "--license -lic", "--terminateOtherVersions -tov", "--deploymentTimeoutMinutes -to", "--deploymentTimeoutMinutesToGetSomeMBMsg -tombm",
            "--terminateOtherVersionsRemove -tovr", "--terminateOtherVersionsRemoveVolumes -tovrv"] },
        { "download-artifacts", ["--verbose -v", "--name -n REQUIRED", "--output -o"] },
        { "install-app", ["--verbose -v", "--replace-tokens", "--name -n REQUIRED", "--app-version -av REQUIRED", "--customer-environment -ce REQUIRED",
            "--license -lic REQUIRED", "--parameters -params", "--output -o", "--timeout -to", "--timeoutToGetSomeMBMsg -tombm"] },
        { "login", ["--verbose -v", "--pat --token -t"] },
        { "publish", ["--verbose -v", "--replace-tokens", "--path -p REQUIRED", "--datagroup -dg"] },
        { "publish-package", ["--verbose -v", "--path -p REQUIRED", "--datagroup -dg"] },
        { "undeploy", ["--verbose -v", "--name -n REQUIRED", "--force -f"] },
        { "uninstall-app", ["--verbose -v", "--name -n REQUIRED", "--customer-environment -ce REQUIRED", "--terminateOtherVersionsRemove -tovr",
            "--removeVolumes --terminateOtherVersionsRemoveVolumes -tovrv", "--undeploy", "--timeout -to", "--timeoutToGetSomeMBMsg -tombm"] },
    };

    public static TheoryData<string, Type> LegacyCommandLines => new()
    {
        { "login", typeof(LoginCommand) },
        { "login --token abc -v", typeof(LoginCommand) },
        { "deploy", typeof(DeployCommand) },
        { "deploy -params \"{file}\" --verbose -lic \"CMF - PT_Development_v8.0.0_20210913\" --site \"CMF - PT\" --target \"portainer\" --package=\"@criticalmanufacturing\\mes-runtime\" --replace-tokens DockerSwarmServerPassword=MyPassword CloudflareAPIToken=myCloudflareToken", typeof(DeployCommand) },
        { "deploy -params \"{file}\" --verbose -lic \"CMF - PT_Development_v8.2.0_ED20250408\" --site \"CMF - PT\" --package=\"@criticalmanufacturing\\mes:8.3.10\" --target \"dockerswarm\" --output \"{dir}\" --deploymentTimeoutMinutes 60 --deploymentTimeoutMinutesToGetSomeMBMsg 60", typeof(DeployCommand) },
        { "deploy -ci \"VMInfrastructureFromPortalSDK\" -n \"TestEnvFromPortalSDK9\" -d \"lala from console\" -params \"{file}\" -type Development -lic \"CMF - PT_Development_v8.0.0_20220407\" --package=\"@criticalmanufacturing\\mes:8.2.3\" -trg \"dockerswarm\" -o \".\"", typeof(DeployCommand) },
        { "deploy --name env -tov -tovr -tovrv -to 10 -tombm 5 -i -a alias", typeof(DeployCommand) },
        { "deployagent -params \"{file}\" --verbose --target \"portainer\" --replace-tokens DockerSwarmServerPassword=MyPassword CloudflareAPIToken=myCloudflareToken", typeof(DeployAgentCommand) },
        { "createinfrastructure --name \"sdkTestInfra\" --customer \"VIRTUAL_CMF\" --verbose --ignore-if-exists", typeof(CreateInfrastructureCommand) },
        { "publish --path \"{dir}\" --datagroup \"\" --verbose --replace-tokens TargetVersion=1.1.1 ImageTag=development-83x ImageRegistry=dev.criticalmanufacturing.io", typeof(PublishCommand) },
        { "publish-package --path \"{file}\" --datagroup \"\" --verbose", typeof(PublishPackageCommand) },
        { "checkagentconnection -n \"sdkTestInfra\"", typeof(CheckAgentConnectionCommand) },
        { "checkagentconnection --agent-name agent", typeof(CheckAgentConnectionCommand) },
        { "checkagentconnection -ce env", typeof(CheckAgentConnectionCommand) },
        { "install-app -n App -av 1.0.0 -ce env -lic License -params \"{file}\" -o \"{dir}\" -to 10 -tombm 5 --replace-tokens A=1", typeof(InstallAppCommand) },
        { "uninstall-app --customer-environment \"env\" --name \"Dummy\"", typeof(UninstallAppCommand) },
        { "uninstall-app -ce env -n Dummy --removeVolumes --undeploy -to 10 -tombm 5 -tovr", typeof(UninstallAppCommand) },
        { "undeploy -n env -f", typeof(UndeployCommand) },
        { "download-artifacts -n env -o \"{dir}\"", typeof(DownloadArtifactsCommand) },
    };

    [Theory]
    [MemberData(nameof(LegacyOptions))]
    public void LegacyCommand_KeepsItsOptionAliasesAndRequiredFlags(string commandName, string[] expectedOptions)
    {
        // Arrange
        Command command = RootCommandFactory.Create().Subcommands.Single(c => c.Name == commandName);

        // Act
        var actualOptions = command.Options.Select(o => Normalize(o.Aliases.Append(o.Name), o.Required)).Order().ToArray();

        // Assert
        Assert.Equal(expectedOptions.Select(e => Normalize(e.Split(' ').Where(t => t != "REQUIRED"), e.EndsWith(" REQUIRED"))).Order().ToArray(), actualOptions);
    }

    [Theory]
    [MemberData(nameof(LegacyCommandLines))]
    public void LegacyCommandLine_ParsesWithoutErrors(string commandLine, Type expectedCommand)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.Empty(result.Errors);
        Assert.IsType(expectedCommand, result.CommandResult.Command);
    }

    [Fact]
    public void Deploy_TypeDefaultsToDevelopment()
    {
        // Act
        ParseResult result = fixture.Parse("deploy");

        // Assert
        Assert.Equal("Development", result.GetValue<string>("--type"));
    }

    [Fact]
    public void Deploy_ReadsMultiLetterSingleDashAliasesAsSingleOptions()
    {
        // Act
        ParseResult result = fixture.Parse("deploy -ci infra -params \"{file}\" -type Production -trg dockerswarm -pck package -lic \"A, B\" -tov -to 10 -tombm 5 -tovr -tovrv");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal("infra", result.GetValue<string>("--customer-infrastructure-name"));
        Assert.Equal(fixture.ExistingFile, result.GetValue<FileInfo>("--parameters")!.FullName);
        Assert.Equal("Production", result.GetValue<string>("--type"));
        Assert.Equal("dockerswarm", result.GetValue<string>("--target"));
        Assert.Equal("package", result.GetValue<string>("--package"));
        Assert.Equal(["A", "B"], result.GetValue<string[]>("--license"));
        Assert.True(result.GetValue<bool>("--terminateOtherVersions"));
        Assert.Equal(10, result.GetValue<double?>("--deploymentTimeoutMinutes"));
        Assert.Equal(5, result.GetValue<double?>("--deploymentTimeoutMinutesToGetSomeMBMsg"));
        Assert.True(result.GetValue<bool>("--terminateOtherVersionsRemove"));
        Assert.True(result.GetValue<bool>("--terminateOtherVersionsRemoveVolumes"));
    }

    [Theory]
    [InlineData("undeploy")]
    [InlineData("install-app -av 1 -ce env -lic L")]
    [InlineData("uninstall-app -n app")]
    [InlineData("publish")]
    [InlineData("publish-package --path \"{dir}\\missing.zip\"")]
    [InlineData("download-artifacts")]
    [InlineData("deploy -type Unknown")]
    [InlineData("deploy -trg unknown")]
    [InlineData("checkagentconnection")]
    [InlineData("checkagentconnection -n agent -ce env")]
    public void LegacyCommandLine_WithInvalidInput_HasErrors(string commandLine)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.NotEmpty(result.Errors);
    }

    [Theory]
    [InlineData("checkagentconnection")]
    [InlineData("createinfrastructure")]
    [InlineData("deployagent")]
    [InlineData("download-artifacts")]
    [InlineData("install-app")]
    [InlineData("publish-package")]
    [InlineData("uninstall-app")]
    public void LegacyOnlyCommand_IsListedInHelp(string commandName)
    {
        // Act
        Command command = RootCommandFactory.Create().Subcommands.Single(c => c.Name == commandName);

        // Assert
        Assert.False(command.Hidden);
    }

    [Theory]
    [InlineData("deploy")]
    [InlineData("undeploy")]
    [InlineData("publish")]
    public void LegacyCommandUsedAsVerb_HidesLegacyOptionsExceptVerbose(string commandName)
    {
        // Act
        Command command = RootCommandFactory.Create().Subcommands.Single(c => c.Name == commandName);

        // Assert
        Assert.False(command.Hidden);
        Assert.All(command.Options, o => Assert.Equal(o.Name != "--verbose", o.Hidden));
    }

    private static string Normalize(IEnumerable<string> aliases, bool required)
    {
        return string.Join(" ", aliases.Order(StringComparer.Ordinal)) + (required ? " REQUIRED" : "");
    }
}
