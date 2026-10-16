using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Commands.Cancel;
using Cmf.CustomerPortal.Sdk.Console.Commands.Create;
using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using Cmf.CustomerPortal.Sdk.Console.Commands.Download;
using Cmf.CustomerPortal.Sdk.Console.Commands.Get;
using Cmf.CustomerPortal.Sdk.Console.Commands.Healthcheck;
using Cmf.CustomerPortal.Sdk.Console.Commands.Logs;
using Cmf.CustomerPortal.Sdk.Console.Commands.Publish;
using Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy;
using System.CommandLine;

namespace Console.UnitTests;

/// <summary>
/// The <c>verb noun [name] [options]</c> commands.
/// </summary>
public class VerbNounCommandTests(CommandLineFixture fixture) : IClassFixture<CommandLineFixture>
{
    public static TheoryData<string, Type> CommandLines => new()
    {
        { "deploy env", typeof(DeployEnvironmentCommand) },
        { "deploy env my-env -params \"{file}\" -lic \"L1,L2\" -s site -pck package -trg dockerswarm --replace-tokens A=1 B=2", typeof(DeployEnvironmentCommand) },
        { "deploy environment my-env -ci infra -d desc -type Staging -o \"{dir}\" -i -tov -tovr -tovrv -to 10 -tombm 5", typeof(DeployEnvironmentCommand) },
        { "deploy agent", typeof(DeployAgentCommand) },
        { "deploy agent my-agent -params \"{file}\" -trg portainer --replace-tokens A=1", typeof(DeployAgentCommand) },
        { "deploy app my-app -ce env -av 1.0.0 -lic License", typeof(DeployAppCommand) },
        { "deploy application my-app -ce env -av 1.0.0 -lic License -params \"{file}\" -o \"{dir}\" -to 10 -tombm 5 --replace-tokens A=1", typeof(DeployAppCommand) },
        { "undeploy env my-env", typeof(UndeployEnvironmentCommand) },
        { "undeploy environment my-env --force", typeof(UndeployEnvironmentCommand) },
        { "undeploy app my-app -ce env", typeof(UndeployAppCommand) },
        { "undeploy application my-app -ce env --removeVolumes --undeploy -to 10 -tombm 5", typeof(UndeployAppCommand) },
        { "cancel env my-env", typeof(CancelEnvironmentCommand) },
        { "cancel environment my-env", typeof(CancelEnvironmentCommand) },
        { "cancel app my-app -ce env", typeof(CancelAppCommand) },
        { "cancel application my-app --customer-environment env", typeof(CancelAppCommand) },
        { "create infrastructure", typeof(CreateInfrastructureCommand) },
        { "create infra my-infra --customer customer --ignore-if-exists -params \"{file}\"", typeof(CreateInfrastructureCommand) },
        { "healthcheck agent my-agent", typeof(HealthcheckAgentCommand) },
        { "healthcheck agent -ce env", typeof(HealthcheckAgentCommand) },
        { "publish deploymentpackage \"{dir}\" -dg datagroup --replace-tokens A=1", typeof(PublishDeploymentPackageCommand) },
        { "publish installationpackage \"{file}\" -dg datagroup", typeof(PublishInstallationPackageCommand) },
        { "download artifacts my-env -o \"{dir}\"", typeof(DownloadArtifactsCommand) },
        { "get env my-env", typeof(GetEnvironmentCommand) },
        { "get environment my-env -ver 3 -f json", typeof(GetEnvironmentCommand) },
        { "get env my-env --version 3 --format Table -v", typeof(GetEnvironmentCommand) },
        { "get deploymentpackage @criticalmanufacturing\\mes:12.0.0", typeof(GetDeploymentPackageCommand) },
        { "get deploymentpackage \"@criticalmanufacturing\\mes:12.0.0\" -v", typeof(GetDeploymentPackageCommand) },
        { "logs env my-env", typeof(LogsEnvironmentCommand) },
        { "logs environment my-env -ver 3 --type termination -v", typeof(LogsEnvironmentCommand) },
        { "logs env my-env -o logs.json", typeof(LogsEnvironmentCommand) },
        { "logs env my-env --output \"{dir}\" --type installation", typeof(LogsEnvironmentCommand) },
        { "logs env my-env -f json -o logs.json", typeof(LogsEnvironmentCommand) },
        { "logs environment my-env --format Table", typeof(LogsEnvironmentCommand) },
    };

    [Theory]
    [MemberData(nameof(CommandLines))]
    public void CommandLine_ParsesWithoutErrors(string commandLine, Type expectedCommand)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.Empty(result.Errors);
        Assert.IsType(expectedCommand, result.CommandResult.Command);
    }

    [Theory]
    [InlineData("deploy env my-env", "name", "my-env")]
    [InlineData("deploy env my-env -lic L --replace-tokens A=1 B=2", "name", "my-env")]
    [InlineData("deploy agent my-agent", "name", "my-agent")]
    [InlineData("deploy app my-app -ce env -av 1 -lic L", "name", "my-app")]
    [InlineData("undeploy env my-env", "name", "my-env")]
    [InlineData("undeploy app my-app -ce env", "name", "my-app")]
    [InlineData("cancel env my-env", "name", "my-env")]
    [InlineData("cancel app my-app -ce env", "name", "my-app")]
    [InlineData("create infrastructure my-infra", "name", "my-infra")]
    [InlineData("healthcheck agent my-agent", "agent-name", "my-agent")]
    [InlineData("download artifacts my-env", "name", "my-env")]
    [InlineData("get env my-env", "name", "my-env")]
    [InlineData("get deploymentpackage my-package:1.0.0", "name", "my-package:1.0.0")]
    [InlineData("get deploymentpackage @criticalmanufacturing\\mes:12.0.0", "name", "@criticalmanufacturing\\mes:12.0.0")]
    [InlineData("logs env my-env", "name", "my-env")]
    public void CommandLine_ReadsNameFromPositionalArgument(string commandLine, string argumentName, string expectedName)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(expectedName, result.GetValue<string>(argumentName));
    }

    [Fact]
    public void DeployEnvironment_WithoutName_LeavesNameEmptySoItIsGenerated()
    {
        // Act
        ParseResult result = fixture.Parse("deploy env -lic L");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Null(result.GetValue<string>("name"));
    }

    [Fact]
    public void DeployEnvironment_DoesNotConsumeNameAsLicense()
    {
        // Act
        ParseResult result = fixture.Parse("deploy env -lic \"A, B\" my-env");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(["A", "B"], result.GetValue<string[]>("--license"));
        Assert.Equal("my-env", result.GetValue<string>("name"));
    }

    [Fact]
    public void PublishDeploymentPackage_ReadsPathFromPositionalArgument()
    {
        // Act
        ParseResult result = fixture.Parse("publish deploymentpackage \"{dir}\"");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(fixture.ExistingDirectory, result.GetValue<FileSystemInfo>("path")!.FullName);
    }

    [Theory]
    [InlineData("deploy env --name my-env")]
    [InlineData("deploy env one two")]
    [InlineData("deploy env -type Unknown")]
    [InlineData("deploy app my-app -av 1 -lic L")]
    [InlineData("deploy app -ce env -av 1 -lic L")]
    [InlineData("undeploy env")]
    [InlineData("undeploy app my-app")]
    [InlineData("cancel env")]
    [InlineData("cancel env one two")]
    [InlineData("cancel app my-app")]
    [InlineData("cancel app -ce env")]
    [InlineData("healthcheck agent")]
    [InlineData("healthcheck agent my-agent -ce env")]
    [InlineData("publish deploymentpackage \"{dir}\\missing\"")]
    [InlineData("publish installationpackage")]
    [InlineData("download artifacts")]
    [InlineData("get env")]
    [InlineData("get env my-env -f xml")]
    [InlineData("get env my-env -ver abc")]
    [InlineData("get env my-env -ver 0")]
    [InlineData("get deploymentpackage")]
    [InlineData("get deploymentpackage one two")]
    [InlineData("get deploymentpackage my-package -f json")]
    [InlineData("get deploymentpackage my-package --format Table")]
    [InlineData("logs env")]
    [InlineData("logs env my-env --type other")]
    [InlineData("logs env my-env --version -1")]
    [InlineData("logs env my-env -f xml")]
    [InlineData("cancel")]
    [InlineData("get")]
    [InlineData("logs")]
    [InlineData("create")]
    [InlineData("healthcheck")]
    [InlineData("download")]
    [InlineData("undeploy -f env my-env")]
    [InlineData("undeploy -n x env my-env")]
    [InlineData("undeploy -v env my-env")]
    [InlineData("deploy -lic L env my-env")]
    [InlineData("publish -dg g deploymentpackage \"{dir}\"")]
    public void CommandLine_WithInvalidInput_HasErrors(string commandLine)
    {
        // Act
        ParseResult result = fixture.Parse(commandLine);

        // Assert
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void VerbOptionBeforeNoun_IsUnrecognized()
    {
        // Act
        ParseResult result = fixture.Parse("undeploy -f env my-env");

        // Assert
        Assert.Equal("Unrecognized command or argument '-f'.", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void HealthcheckAgent_WithoutAgent_ReportsWhichOptionsAreMissing()
    {
        // Act
        ParseResult result = fixture.Parse("healthcheck agent");

        // Assert
        Assert.Equal("At least one of the following options must be provided: <agent-name>, --customer-environment", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void GetEnvironment_ReadsVersionAndFormat()
    {
        // Act
        ParseResult result = fixture.Parse("get env my-env -ver 3 -f json");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(3, result.GetValue<int?>("--version"));
        Assert.Equal(OutputFormat.Json, result.GetValue<OutputFormat>("--format"));
    }

    [Fact]
    public void GetEnvironment_DefaultsToTheCurrentVersionAsATable()
    {
        // Act
        ParseResult result = fixture.Parse("get env my-env");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Null(result.GetValue<int?>("--version"));
        Assert.Equal(OutputFormat.Table, result.GetValue<OutputFormat>("--format"));
    }

    [Fact]
    public void LogsEnvironment_DefaultsToAllLogsAsATable()
    {
        // Act
        ParseResult result = fixture.Parse("logs env my-env");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(EnvironmentLogType.All, result.GetValue<EnvironmentLogType>("--type"));
        Assert.Equal(OutputFormat.Table, result.GetValue<OutputFormat>("--format"));
        Assert.Null(result.GetValue<string>("--output"));
    }

    [Fact]
    public void LogsEnvironment_ReadsTheFormat()
    {
        // Act
        ParseResult result = fixture.Parse("logs env my-env -f json");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal(OutputFormat.Json, result.GetValue<OutputFormat>("--format"));
    }

    [Fact]
    public void LogsEnvironment_ReadsTheOutputPath()
    {
        // Act
        ParseResult result = fixture.Parse("logs env my-env -o out/logs.json");

        // Assert
        Assert.Empty(result.Errors);
        Assert.Equal("out/logs.json", result.GetValue<string>("--output"));
    }

    [Fact]
    public void EnvironmentVersion_BelowOne_ReportsThatItMustBePositive()
    {
        // Act
        ParseResult result = fixture.Parse("logs env my-env -ver 0");

        // Assert
        Assert.Equal("The version must be a positive number", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void RootVersion_StillShowsTheCliVersion()
    {
        // Act
        ParseResult result = fixture.Parse("--version");

        // Assert
        Assert.Empty(result.Errors);
        Assert.IsType<RootCommand>(result.CommandResult.Command);
    }
}
