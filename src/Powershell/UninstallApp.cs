using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell;

[Cmdlet(VerbsLifecycle.Uninstall, "App")]
public class UninstallApp : BaseCmdlet<UninstallAppHandler>
{
    [Parameter(HelpMessage = Resources.APP_UNINSTALL_NAME_HELP, Mandatory = true)]
    public string Name { get; set; }

    [Parameter(HelpMessage = Resources.APP_NAME_HELP, Mandatory = true)]
    public string CustomerEnvironment { get; set; }

    [Parameter(HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_HELP)]
    public SwitchParameter TerminateOtherVersionsRemove; // unused, kept for compatibility

    [Parameter(HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP)]
    public SwitchParameter TerminateOtherVersionsRemoveVolumes;
    
    [Parameter(HelpMessage = Resources.APP_UNDEPLOY_HELP)]
    public SwitchParameter Undeploy { get; set; }

    [Parameter(HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES)]
    public double? DeploymentTimeoutMinutes { get; set; }

    [Parameter(HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE)]
    public double? DeploymentTimeoutMinutesToGetSomeMBMsg { get; set; }


    protected override async Task ProcessRecordAsync()
    {
        // get uninstall app handler and run it
        UninstallAppHandler uninstallAppHandler = ServiceLocator.Get<UninstallAppHandler>();
        await uninstallAppHandler.Run(
            Name,
            CustomerEnvironment,
            TerminateOtherVersionsRemoveVolumes,
            Undeploy,
            DeploymentTimeoutMinutes,
            DeploymentTimeoutMinutesToGetSomeMBMsg
        );
    }
}
