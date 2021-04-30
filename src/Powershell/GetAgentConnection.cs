using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Get, "AgentConnection")]
    public class GetAgentConnection : BaseCmdlet<GetAgentConnectionHandler>
    {
        [Parameter(
            HelpMessage = Resources.GETAGENTCONNECTION_NAME_HELP,
            Mandatory = true
        )]
        public string Name { get; set; }

        protected async override Task ProcessRecordAsync()
        {
            // get GetAgentConnectionHandler and run it
            GetAgentConnectionHandler getAgentConnectionHandler = ServiceLocator.Get<GetAgentConnectionHandler>();
            bool result = await getAgentConnectionHandler.Run(Name);

            WriteObject(result, false);
        }
    }
}
