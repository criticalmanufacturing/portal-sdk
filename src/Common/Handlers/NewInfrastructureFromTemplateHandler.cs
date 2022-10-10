using Cmf.CustomerPortal.BusinessObjects;
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

        public async Task Run(string infrastructureName, string infrastructureTemplateName, bool force, int? secondsTimeout)
        {
            await EnsureLogin();

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

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

            // check if the current customerInfrastructureName already exists and continue deppending on the value of Force variable
            CustomerInfrastructure customerInfrastructure = await InfrastructureUtilities.CheckCustomerInfrastructureAlreadyExists(Session, _customerPortalClient, force, customerInfrastructureName);

            if (customerInfrastructure == null)
            {
                // create customer infrastructure if doesn't exists.
                customerInfrastructure = await InfrastructureUtilities.CreateCustomerInfrastructure(Session, customerInfrastructureTemplate.Customer, customerInfrastructureName, customerInfrastructureTemplate.Parameters, templates);
            }

            // wait if necessary to Unlock Customer Infrastructure
            await InfrastructureUtilities.WaitForCustomerInfrastructureUnlockAsync(Session, _customerPortalClient, customerInfrastructure, secondsTimeout);

            InfrastructureUtilities.GetInfrastructureUrl(Session, customerInfrastructure);
        }
    }
}
