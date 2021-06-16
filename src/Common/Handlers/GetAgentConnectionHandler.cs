﻿using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class GetAgentConnectionHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public GetAgentConnectionHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        public async Task<bool> Run(string agentName)
        {
            await EnsureLogin();

            CustomerEnvironment agent = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(agentName);
            return agent.CurrentMainState.CurrentState.Name == "Connected";
        }
    }
}
