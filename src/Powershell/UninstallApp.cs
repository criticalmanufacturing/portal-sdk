using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Reset, "Environment")]
    public class UninstallApp : BaseCmdlet<UninstallAppHandler>
    {
        [Parameter(
            HelpMessage = Resources.APP_NAME_HELP,
            Mandatory = true,
            Position = 1
        )]

        public string Name { get; set; }
        [Parameter(
            HelpMessage = Resources.CUSTOMER_ENVIRONMENT_NAME_HELP,
            Mandatory = true,
            Position = 2
        )]
        public string CustomerEnvironment { get; set; }

        [Parameter(Position = 3, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_HELP)]
        public SwitchParameter TerminateOtherVersionsRemove;

        [Parameter(Position = 4, HelpMessage = Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP)]
        public SwitchParameter TerminateOtherVersionsRemoveVolumes;
        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES,
            Mandatory = false,
            Position = 5
        )]
        public double? DeploymentTimeoutMinutes { get; set; }

        [Parameter(
            HelpMessage = Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE,
            Mandatory = false,
            Position = 6
        )]
        public double? DeploymentTimeoutMinutesToGetSomeMBMsg { get; set; }


        protected async override Task ProcessRecordAsync()
        {
            // get uninstall app handler and run it
            UninstallAppHandler uninstallAppHandler = ServiceLocator.Get<UninstallAppHandler>();
            await uninstallAppHandler.Run(Name, CustomerEnvironment, TerminateOtherVersionsRemove.ToBool(), TerminateOtherVersionsRemoveVolumes.ToBool(), DeploymentTimeoutMinutes, DeploymentTimeoutMinutesToGetSomeMBMsg);
        }
    }
}