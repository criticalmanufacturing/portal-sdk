using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System;
using System.CommandLine;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Get;

/// <summary>
/// <c>get env &lt;name&gt; [--version &lt;version&gt;] [--format Table|Json]</c>: shows the details of a
/// Customer Environment version.
/// </summary>
class GetEnvironmentCommand : BaseCommand
{
    private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

    private readonly Option<OutputFormat> formatOption = new("--format", "-f")
    {
        Description = Resources.OutputFormatHelp,
        DefaultValueFactory = _ => OutputFormat.Table
    };

    private readonly EnvironmentVersionExtension version;

    public GetEnvironmentCommand() : base("env", Resources.NounGetEnvironmentHelp)
    {
        Aliases.Add("environment");
        Arguments.Add(nameArgument);
        version = Use(new EnvironmentVersionExtension());
        Options.Add(formatOption);

        SetAction(GetEnvironment);
    }

    private async Task GetEnvironment(ParseResult parseResult)
    {
        CreateSession(parseResult);
        GetEnvironmentHandler handler = ServiceLocator.Get<GetEnvironmentHandler>();
        CustomerEnvironmentDetails details = await handler.Run(parseResult.GetValue(nameArgument), parseResult.GetValue(version.Version));

        TextWriter output = parseResult.InvocationConfiguration.Output;
        if (parseResult.GetValue(formatOption) == OutputFormat.Json)
        {
            output.WriteLine(JsonSerializer.Serialize(details, JsonOutput.Options));
        }
        else
        {
            WriteTable(output, details);
        }
    }

    /// <summary>
    /// Writes the details as <c>Label: value</c> lines with aligned values, skipping the empty ones.
    /// </summary>
    internal static void WriteTable(TextWriter output, CustomerEnvironmentDetails details)
    {
        (string Label, string? Value)[] rows =
        [
            ("Name", details.Name),
            ("Id", details.Id.ToString()),
            ("Version", details.Version.ToString()),
            ("Description", details.Description),
            ("Type", details.EnvironmentType),
            ("Status", details.Status),
            ("Universal State", details.UniversalState),
            ("Site", details.Site),
            ("Customer Infrastructure", details.CustomerInfrastructure),
            ("Deployment Package", details.DeploymentPackage),
            ("Deployment Target", details.DeploymentTarget),
            ("Created", Audit(details.CreatedOn, details.CreatedBy)),
            ("Modified", Audit(details.ModifiedOn, details.ModifiedBy))
        ];

        int width = rows.Max(row => row.Label.Length) + 1;
        foreach ((string label, string? value) in rows.Where(row => row.Value != null))
        {
            output.WriteLine($"{(label + ":").PadRight(width)} {value}");
        }
    }

    private static string? Audit(DateTime on, string? by)
    {
        if (on == default)
        {
            return by;
        }

        string date = on.ToString("yyyy-MM-dd HH:mm:ss");
        return by == null ? date : $"{date} by {by}";
    }
}
