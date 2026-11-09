using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Globalization;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell;

[Cmdlet(VerbsLifecycle.Uninstall, "App")]
public class UninstallApp : BaseCmdlet<UninstallAppHandler>
{
    [Parameter(HelpMessage = Resources.AppUninstallNameHelp, Mandatory = true)]
    public string Name { get; set; }

    [Parameter(HelpMessage = Resources.AppNameHelp, Mandatory = true)]
    public string CustomerEnvironment { get; set; }

    [Parameter(HelpMessage = Resources.DeploymentTerminateOtherVersionsRemoveHelp)]
    public SwitchParameter TerminateOtherVersionsRemove; // unused, kept for compatibility

    [Parameter(HelpMessage = Resources.AppUninstallRemoveVolumesDeprecatedPowershellHelp)]
    public SwitchParameter TerminateOtherVersionsRemoveVolumes; // deprecated, replaced by -Purge

    [Parameter(HelpMessage = Resources.AppUndeployDeprecatedPowershellHelp)]
    public SwitchParameter Undeploy { get; set; } // deprecated, replaced by -Purge

    [Parameter(HelpMessage = Resources.AppUninstallPurgeHelp)]
    public SwitchParameter Purge { get; set; }

    [Parameter(HelpMessage = Resources.DeploymentTimeoutMinutesHelp)]
    public double? DeploymentTimeoutMinutes { get; set; }

    [Parameter(HelpMessage = Resources.DeploymentTimeoutMinutesToGetSomeMBMessageHelp)]
    public double? DeploymentTimeoutMinutesToGetSomeMBMsg { get; set; }


    protected override async Task ProcessRecordAsync()
    {
        if (TerminateOtherVersionsRemoveVolumes.IsPresent)
        {
            WriteWarning(string.Format(CultureInfo.InvariantCulture, Resources.AppUninstallRemoveVolumesDeprecated, $"-{nameof(TerminateOtherVersionsRemoveVolumes)}", $"-{nameof(Purge)}"));
        }

        if (Undeploy.IsPresent)
        {
            WriteWarning(string.Format(CultureInfo.InvariantCulture, Resources.AppUndeployDeprecated, $"-{nameof(Undeploy)}", $"-{nameof(Purge)}"));
        }

        // get uninstall app handler and run it
        UninstallAppHandler uninstallAppHandler = ServiceLocator.Get<UninstallAppHandler>();
        await uninstallAppHandler.Run(
            Name,
            CustomerEnvironment,
            TerminateOtherVersionsRemoveVolumes,
            Undeploy,
            Purge,
            DeploymentTimeoutMinutes,
            DeploymentTimeoutMinutesToGetSomeMBMsg
        );
    }
}
