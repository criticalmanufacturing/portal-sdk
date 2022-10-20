using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
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

        public async Task Run(string infrastructureName, string infrastructureTemplateName, bool ignoreIfExists)
        {
            await EnsureLogin();

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

            // load template it to get its relations (customer environment templates)
            Session.LogDebug($"Loading CustomerInfrastructure template {infrastructureTemplateName}...");
            CustomerEnvironmentCollection templates = new CustomerEnvironmentCollection();

            // load customer infrastructure template
            CustomerInfrastructure customerInfrastructureTemplate = await InfrastructureCreationService.GetCustomerInfrastructure(Session, _customerPortalClient, infrastructureTemplateName, true);

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

            // check if the current customerInfrastructureName already exists and continue deppending on the value of ignoreIfExists variable
            CustomerInfrastructure customerInfrastructure = await InfrastructureCreationService.CheckCustomerInfrastructureAlreadyExists(Session, _customerPortalClient, ignoreIfExists, customerInfrastructureName);

            if (customerInfrastructure == null)
            {
                // create customer infrastructure if doesn't exists.
                customerInfrastructure = await InfrastructureCreationService.CreateCustomerInfrastructure(Session, customerInfrastructureTemplate.Customer, customerInfrastructureName, customerInfrastructureTemplate.Parameters, templates);
            }

            // wait if necessary to Unlock Customer Infrastructure
            await InfrastructureCreationService.WaitForCustomerInfrastructureUnlockAsync(Session, _customerPortalClient, customerInfrastructure);

            InfrastructureCreationService.GetInfrastructureUrl(Session, customerInfrastructure);
        }
    }
}
