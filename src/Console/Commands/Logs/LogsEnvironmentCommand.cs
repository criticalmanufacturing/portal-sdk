using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Logs;

/// <summary>
/// <c>logs env &lt;name&gt; [--version &lt;version&gt;] [--type All|Installation|Termination] [--output &lt;path&gt;]</c>:
/// writes the logs of a Customer Environment version as one JSON object, keyed by log type, to the standard output or
/// to a file.
/// </summary>
class LogsEnvironmentCommand : BaseCommand
{
    private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

    private readonly Option<EnvironmentLogType> typeOption = new("--type")
    {
        Description = Resources.LogTypeHelp,
        DefaultValueFactory = _ => EnvironmentLogType.All
    };

    private readonly Option<string> outputOption = new("--output", "-o")
    {
        Description = Resources.LogsOutputHelp,
        HelpName = "path"
    };

    private readonly EnvironmentVersionExtension version;

    public LogsEnvironmentCommand() : base("env", Resources.NounLogsEnvironmentHelp)
    {
        Aliases.Add("environment");
        Arguments.Add(nameArgument);
        version = Use(new EnvironmentVersionExtension());
        Options.Add(typeOption);
        Options.Add(outputOption);

        SetAction(GetLogs);
    }

    public override string PreviewMessage => Resources.LogsEnvironmentPreview;

    private async Task GetLogs(ParseResult parseResult)
    {
        string name = parseResult.GetValue(nameArgument);
        int? versionNumber = parseResult.GetValue(version.Version);

        CreateSession(parseResult);
        GetEnvironmentLogsHandler handler = ServiceLocator.Get<GetEnvironmentLogsHandler>();
        JsonObject logs = await handler.Run(name, versionNumber, parseResult.GetValue(typeOption));

        string output = parseResult.GetValue(outputOption);
        if (string.IsNullOrWhiteSpace(output))
        {
            parseResult.InvocationConfiguration.Output.WriteLine(logs.ToJsonString(JsonOutput.Options));
        }
        else
        {
            handler.WriteLogs(logs, output, name, versionNumber);
        }
    }
}
