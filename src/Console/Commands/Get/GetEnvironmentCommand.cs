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

    private readonly EnvironmentVersionExtension version;
    private readonly OutputFormatExtension format;

    public GetEnvironmentCommand() : base("env", Resources.NounGetEnvironmentHelp)
    {
        Aliases.Add("environment");
        Arguments.Add(nameArgument);
        version = Use(new EnvironmentVersionExtension());
        format = Use(new OutputFormatExtension());

        SetAction(GetEnvironment);
    }

    public override string PreviewMessage => Resources.GetEnvironmentPreview;

    private async Task GetEnvironment(ParseResult parseResult)
    {
        CreateSession(parseResult);
        GetEnvironmentHandler handler = ServiceLocator.Get<GetEnvironmentHandler>();
        CustomerEnvironmentDetails details = await handler.Run(parseResult.GetValue(nameArgument), parseResult.GetValue(version.Version));

        TextWriter output = parseResult.InvocationConfiguration.Output;
        if (parseResult.GetValue(format.Format) == OutputFormat.Json)
        {
            output.WriteLine(JsonSerializer.Serialize(details, JsonOutput.Options));
        }
        else
        {
            WriteTable(output, details);
        }
    }

    /// <summary>
    /// Writes the details as <c>Label: value</c> lines with aligned values: one line per field of the JSON output,
    /// with <see cref="TableOutput.EmptyValue"/> for the empty ones.
    /// </summary>
    internal static void WriteTable(TextWriter output, CustomerEnvironmentDetails details)
    {
        (string Label, string Value)[] rows =
        [
            ("Id", details.Id.ToString()),
            ("Name", details.Name),
            ("Version", details.Version.ToString()),
            ("Description", details.Description),
            ("Environment Type", details.EnvironmentType),
            ("Status", details.Status),
            ("Universal State", details.UniversalState),
            ("Site", details.Site),
            ("Customer Infrastructure", details.CustomerInfrastructure),
            ("Deployment Package", details.DeploymentPackage),
            ("Deployment Target", details.DeploymentTarget),
            ("Created By", details.CreatedBy),
            ("Created On", FormatDate(details.CreatedOn)),
            ("Modified By", details.ModifiedBy),
            ("Modified On", FormatDate(details.ModifiedOn))
        ];

        int width = rows.Max(row => row.Label.Length) + 1;
        foreach ((string label, string value) in rows)
        {
            output.WriteLine($"{(label + ":").PadRight(width)} {value ?? TableOutput.EmptyValue}");
        }
    }

    private static string FormatDate(DateTime date) => date == default ? null : date.ToString("yyyy-MM-dd HH:mm:ss");
}
