using Cmf.CustomerPortal.BusinessObjects;
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

        public async Task Run(string infrastructureName, string siteName, string customerName, bool force, int? secondsTimeout)
        {
            await EnsureLogin();

            // validate parameters
            if (string.IsNullOrEmpty(customerName) && string.IsNullOrEmpty(siteName))
            {
                Exception error = new Exception("Customer is required to create a new infrastructure");
                Session.LogError(error);
                throw error;
            }

            ProductCustomer customer = null;
            // Fetch customer name from siteName
            if (string.IsNullOrEmpty(customerName))
            {
                // Site name was supplied, load the customerName
                ProductSite site = await _customerPortalClient.GetObjectByName<ProductSite>(siteName, 1);
                if (site != null && site.Customer != null)
                {
                    customer = site.Customer;
                }
                else
                {
                    Exception error = new Exception("Unable to load customer from supplied site");
                    Session.LogError(error);
                    throw error;
                }
            }
            else
            {
                //todo retornar frieldy exception quando nao existe :)
                customer = await _customerPortalClient.GetObjectByName<ProductCustomer>(customerName);
            }

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

            // check if the current customerInfrastructureName already exists and continue deppending on the value of Force variable
            CustomerInfrastructure customerInfrastructure = await InfrastructureUtilities.CheckCustomerInfrastructureAlreadyExists(Session, _customerPortalClient, force, customerInfrastructureName);

            if (customerInfrastructure == null)
            {
                // create customer infrastructure if doesn't exists.
                customerInfrastructure = await InfrastructureUtilities.CreateCustomerInfrastructure(Session, customer, customerInfrastructureName);
            }

            // wait if necessary to Unlock Customer Infrastructure
            await InfrastructureUtilities.WaitForCustomerInfrastructureUnlockAsync(Session, _customerPortalClient, customerInfrastructure, secondsTimeout);

            InfrastructureUtilities.GetInfrastructureUrl(Session, customerInfrastructure);
        }
    }
}
