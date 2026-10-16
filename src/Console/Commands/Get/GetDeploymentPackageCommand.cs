using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Get;

/// <summary>
/// <c>get deploymentpackage &lt;name&gt;</c>: shows the details of a Deployment Package.
/// </summary>
class GetDeploymentPackageCommand : BaseCommand
{
    private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgDeploymentPackageNameHelp };

    public GetDeploymentPackageCommand() : base("deploymentpackage", Resources.NounGetDeploymentPackageHelp)
    {
        Arguments.Add(nameArgument);

        SetAction(GetDeploymentPackage);
    }

    public override string PreviewMessage => Resources.GetDeploymentPackagePreview;

    private async Task GetDeploymentPackage(ParseResult parseResult)
    {
        CreateSession(parseResult);
        GetDeploymentPackageHandler handler = ServiceLocator.Get<GetDeploymentPackageHandler>();
        DeploymentPackageDetails details = await handler.Run(parseResult.GetValue(nameArgument));

        WriteTable(parseResult.InvocationConfiguration.Output, details);
    }

    /// <summary>
    /// Writes the details as <c>Label: value</c> lines with aligned values: one line per field of the details,
    /// with <see cref="TableOutput.EmptyValue"/> for the empty ones.
    /// </summary>
    internal static void WriteTable(TextWriter output, DeploymentPackageDetails details)
    {
        (string Label, string Value)[] rows =
        [
            ("Id", details.Id.ToString()),
            ("Name", details.Name),
            ("Display Name", details.DisplayName),
            ("Description", details.Description),
            ("Package Version", details.PackageVersion),
            ("Universal State", details.UniversalState),
            ("Is Root Package", details.IsRootPackage?.ToString()),
            ("Is Installable", details.IsInstallable?.ToString()),
            ("Pre-Release", details.PreRelease?.ToString()),
            ("Manifest Version", details.ManifestVersion),
            ("License Version", details.LicenseVersion),
            ("Documentation URL", details.DocumentationUrl),
            ("Created By", details.CreatedBy),
            ("Created On", TableOutput.FormatDate(details.CreatedOn)),
            ("Modified By", details.ModifiedBy),
            ("Modified On", TableOutput.FormatDate(details.ModifiedOn))
        ];

        TableOutput.WriteRows(output, rows);
    }
}
