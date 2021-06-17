using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewInfrastructureHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;
        
        public NewInfrastructureHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            this._customerPortalClient = customerPortalClient;
        }

        public async Task Run(string infrastructureName, string agentName, string siteName, string domain)
        {
            await EnsureLogin();

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

            Session.LogInformation($"Creating Customer Infrastructure {customerInfrastructureName}...");

            // create infrastructure
            CustomerInfrastructure customerInfrastructure = new CustomerInfrastructure
            {
                Name = customerInfrastructureName,
                Site = await _customerPortalClient.GetObjectByName<ProductSite>(siteName),
                Domain = domain,
                //Parameters = @"{""SYSTEM_NAME"" : { ""Value"": ""xpto"" }}",
                InfrastructureAgent = string.IsNullOrWhiteSpace(agentName) ? null : await _customerPortalClient.GetObjectByName<CustomerEnvironment>(agentName)
            };

            customerInfrastructure = (await new CreateCustomerInfrastructureInput
            {
                CustomerInfrastructure = customerInfrastructure,
            }.CreateCustomerInfrastructureAsync(true)).CustomerInfrastructure;

            string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerInfrastructure/{customerInfrastructure.Id}";
            Session.LogInformation($"CustomerInfrastructure {customerInfrastructureName} accessible at {infrastructureUrl}");
        }
    }
}
