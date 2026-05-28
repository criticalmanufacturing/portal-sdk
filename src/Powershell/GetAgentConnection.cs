using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Get, "AgentConnection", DefaultParameterSetName = "ByName")]
    public class GetAgentConnection : BaseCmdlet<GetAgentConnectionHandler>
    {
        [Parameter(
            Position = 0,
            ParameterSetName = "ByName",
            Mandatory = true,
            HelpMessage = Resources.GETAGENTCONNECTION_NAME_HELP)]
        public string Name { get; set; }

        [Parameter(
            Position = 0,
            ParameterSetName = "ByCustomerEnvironment",
            Mandatory = true,
            HelpMessage = Resources.GETAGENTCONNECTION_CUSTOMER_ENVIRONMENT_HELP)]
        public string CustomerEnvironment { get; set; }

        protected override async Task ProcessRecordAsync()
        {
            GetAgentConnectionHandler handler = ServiceLocator.Get<GetAgentConnectionHandler>();
            var result = await handler.Run(Name ?? string.Empty, CustomerEnvironment ?? string.Empty);
            WriteObject(result);
        }
    }
}