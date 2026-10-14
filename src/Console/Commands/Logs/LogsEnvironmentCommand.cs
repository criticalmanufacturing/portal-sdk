using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Logs;

/// <summary>
/// <c>logs env &lt;name&gt; [--version &lt;version&gt;] [--type All|Installation|Termination]</c>: writes the
/// logs of a Customer Environment version as one JSON object, keyed by log type.
/// </summary>
class LogsEnvironmentCommand : BaseCommand
{
    private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

    private readonly Option<EnvironmentLogType> typeOption = new("--type")
    {
        Description = Resources.LogTypeHelp,
        DefaultValueFactory = _ => EnvironmentLogType.All
    };

    private readonly EnvironmentVersionExtension version;

    public LogsEnvironmentCommand() : base("env", Resources.NounLogsEnvironmentHelp)
    {
        Aliases.Add("environment");
        Arguments.Add(nameArgument);
        version = Use(new EnvironmentVersionExtension());
        Options.Add(typeOption);

        SetAction(GetLogs);
    }

    private async Task GetLogs(ParseResult parseResult)
    {
        CreateSession(parseResult);
        GetEnvironmentLogsHandler handler = ServiceLocator.Get<GetEnvironmentLogsHandler>();
        JsonObject logs = await handler.Run(parseResult.GetValue(nameArgument), parseResult.GetValue(version.Version), parseResult.GetValue(typeOption));

        parseResult.InvocationConfiguration.Output.WriteLine(logs.ToJsonString(JsonOutput.Options));
    }
}
