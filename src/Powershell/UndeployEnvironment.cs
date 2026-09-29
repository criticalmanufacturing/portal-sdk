using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Reset, "Environment")]
    public class UndeployEnvironment : BaseCmdlet<UndeployEnvironmentHandler>
    {
        [Parameter(
            HelpMessage = Resources.CustomerEnvironmentNameHelp,
            Mandatory = true
        )]
        public string Name { get; set; }

        [Parameter(Position = 1, HelpMessage = Resources.UndeploymentForceHelp)]
        public SwitchParameter Force;

        protected async override Task ProcessRecordAsync()
        {
            // get undeploy environment handler and run it
            UndeployEnvironmentHandler undeployEnvironmentHandler = ServiceLocator.Get<UndeployEnvironmentHandler>();
            await undeployEnvironmentHandler.Run(Name, Force.ToBool());
        }
    }
}