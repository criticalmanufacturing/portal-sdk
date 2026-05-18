using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class GetAgentConnectionHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session,
        ICustomerEnvironmentServices customerEnvironmentServices)
        : AbstractHandler(session, true)
    {
        public async Task<bool> Run(string agentName, string customerEnvironmentName = null)
        {
            await EnsureLogin();
             CustomerEnvironment agent = null;          

            agent = !string.IsNullOrEmpty(agentName) 
                ? await customerPortalClient.GetObjectByName<CustomerEnvironment>(agentName) 
                : await customerPortalClient.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName);

            return await customerPortalClient.CheckCustomerEnvironmentConnectionStatus(agent.DefinitionId); 

        }
    }
}
