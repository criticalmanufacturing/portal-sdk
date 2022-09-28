using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewInfrastructureFromTemplateHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public NewInfrastructureFromTemplateHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            this._customerPortalClient = customerPortalClient;
        }

        public async Task Run(string infrastructureName, string infrastructureTemplateName)
        {
            await EnsureLogin();

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

            Session.LogInformation($"Creating Customer Infrastructure {customerInfrastructureName}...");

            // load template it to get its relations (customer environment templates)
            Session.LogDebug($"Loading CustomerInfrastructure template {infrastructureTemplateName}...");
            CustomerEnvironmentCollection templates = new CustomerEnvironmentCollection();

            // load template
            CustomerInfrastructure customerInfrastructureTemplate = await _customerPortalClient.GetObjectByName<CustomerInfrastructure>(infrastructureTemplateName);

            // validate is template ?
            // load template relations
            string relationName = "CustomerInfrastructureCustomerEnvironment";
            customerInfrastructureTemplate = await _customerPortalClient.LoadObjectRelations(customerInfrastructureTemplate,
                new System.Collections.ObjectModel.Collection<string>() { "CustomerInfrastructureCustomerEnvironment" });

            if (customerInfrastructureTemplate.RelationCollection != null && customerInfrastructureTemplate.RelationCollection.Count > 0)
            {
                customerInfrastructureTemplate.RelationCollection.TryGetValue(relationName, out EntityRelationCollection templatesRelations);

                if (templatesRelations?.Count > 0)
                {
                    foreach (EntityRelation relation in templatesRelations)
                    {
                        templates.Add((relation as CustomerInfrastructureCustomerEnvironment).TargetEntity);
                    }
                }
            }

            // create infrastructure
            CustomerInfrastructure customerInfrastructure = new CustomerInfrastructure
            {
                Name = customerInfrastructureName,
                Customer = customerInfrastructureTemplate.Customer,
                Parameters = customerInfrastructureTemplate.Parameters
            };

            customerInfrastructure = (await new CreateCustomerInfrastructureInput
            {
                CustomerInfrastructure = customerInfrastructure,
                TemplatesToAdd = templates
            }.CreateCustomerInfrastructureAsync(true)).CustomerInfrastructure;

            string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerInfrastructure/{customerInfrastructure.Id}";
            Session.LogInformation($"CustomerInfrastructure {customerInfrastructureName} accessible at {infrastructureUrl}");
        }
    }
}
