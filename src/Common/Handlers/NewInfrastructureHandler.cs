using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System;
using System.Collections.Generic;
using System.IO;
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

        public async Task Run(string infrastructureName, string siteName, string customerName, bool ignoreIfExists, FileInfo parameters)
        {
            await EnsureLogin();

            // validate parameters
            if (string.IsNullOrEmpty(customerName) && string.IsNullOrEmpty(siteName))
            {
                Exception error = new Exception("Customer is required to create a new infrastructure");
                Session.LogError(error);
                throw error;
            }

            // Read parameters file
            string infrastructureParameters = null;
            if (parameters != null)
            {
                infrastructureParameters = File.ReadAllText(parameters.FullName);
            }

            ProductCustomer customer;
            // Fetch customer name from siteName
            if (string.IsNullOrEmpty(customerName))
            {
                customer = await GetCustomerBySiteName(siteName);
            }
            else
            {
                customer = await Utilities.GetObjectByNameWithDefaultErrorMessage<ProductCustomer>(Session,
                    _customerPortalClient,
                    customerName,
                    new Dictionary<Foundation.Common.CmfExceptionType, string>()
                        {
                            { Foundation.Common.CmfExceptionType.Db20001, $"The current Product Customer {customerName} doesn't exist on the system or was not found."}
                        },
                    msgInfoBeforeCall: $"Checking if exists the Product Customer {customerName}...");
            }

            // use name or generate one
            string customerInfrastructureName = string.IsNullOrWhiteSpace(infrastructureName) ? $"CustomerInfrastructure-{Guid.NewGuid()}" : infrastructureName;

            // check if the current customerInfrastructureName already exists and continue deppending on the value of ignoreIfExists variable
            CustomerInfrastructure customerInfrastructure = await InfrastructureCreationService.CheckCustomerInfrastructureAlreadyExists(Session, _customerPortalClient, ignoreIfExists, customerInfrastructureName);

            if (customerInfrastructure == null)
            {
                // create customer infrastructure if doesn't exist.
                customerInfrastructure = await InfrastructureCreationService.CreateCustomerInfrastructure(Session, customer, customerInfrastructureName, infrastructureParameters);
            }

            // wait if necessary to Unlock Customer Infrastructure
            await InfrastructureCreationService.WaitForCustomerInfrastructureUnlockAsync(Session, _customerPortalClient, customerInfrastructure);

            InfrastructureCreationService.GetInfrastructureUrl(Session, customerInfrastructure);
        }

        private async Task<ProductCustomer> GetCustomerBySiteName(string siteName)
        {
            ProductCustomer customer;
            ProductSite site = await Utilities.GetObjectByNameWithDefaultErrorMessage<ProductSite>(Session,
                                _customerPortalClient,
                                siteName,
                                new Dictionary<Foundation.Common.CmfExceptionType, string>()
                                    {
                                        { Foundation.Common.CmfExceptionType.Db20001, $"The current Product Site {siteName} doesn't exist on the system or was not found."}
                                    },
                                1,
                                $"Checking if exists the Product Site {siteName}...");

            // Site name was supplied, load the customerName
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

            return customer;
        }
    }
}
