using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class GetAgentConnectionHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session)
        : AbstractHandler(session, true)
    {
        public async Task<bool> Run(string agentName, string customerEnvironmentName = null!)
        {
            await EnsureLogin();
            CustomerEnvironment agent = null!;          

            agent = !string.IsNullOrEmpty(agentName) 
                ? await customerPortalClient.GetObjectByName<CustomerEnvironment>(agentName) 
                : await customerPortalClient.GetCustomerInfrastructureAgentByCustomerEnvironment(customerEnvironmentName);
            
            if(agent == null )
            {
                return false; // cases where environment exists but has no agent or infra associated
            }

            return await customerPortalClient.CheckCustomerEnvironmentConnectionStatus(agent.DefinitionId); 
        }
    }
}
