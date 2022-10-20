using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public class InfrastructureCreationService : Utilities
    {
        private const int _defaultSecondsTimeout = 180;

        /// <summary>
        /// Check if Customer Infrastructure already exists. 
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="customerPortalClient">Client to make requests to the Customer Portal's APIs</param>
        /// <param name="ignoreIfExists">IgnoreIfExist is a variable that indicates if the program can continue if the Customer Infrastructure already exists.</param>
        /// <param name="customerInfrastructureName">Name of Customer Infrastructure</param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> CheckCustomerInfrastructureAlreadyExists(ISession session, ICustomerPortalClient customerPortalClient, bool ignoreIfExists, string customerInfrastructureName)
        {
            CustomerInfrastructure currentCustomerInfrastructure = await GetCustomerInfrastructure(session, customerPortalClient, customerInfrastructureName, false);

            // if customerInfrastructure doesn't exists. Can continue to be created.
            // else check the ignoreIfExists value

            if (currentCustomerInfrastructure != null)
            {
                if (ignoreIfExists)
                {
                    session.LogInformation($"The Customer Infrastructure with name '{customerInfrastructureName}' already exists but the 'ignoreIfExists' flag has been set. Will be used the current Customer Infrastructure.");
                }
                else
                {
                    string errorMessage = $"The Customer Infrastructure with name '{customerInfrastructureName}' already exists and cannot be created. If you want to continue, please use the 'ignoreIfExists' flag on the command.";
                    session.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }

            return currentCustomerInfrastructure;
        }

        /// <summary>
        /// Get Customer Infrastructure
        /// </summary>
        /// <param name="session">session</param>
        /// <param name="customerPortalClient">customer portal client</param>
        /// <param name="customerInfrastructureName">customer infrastructure name</param>
        /// <param name="throwIfNotExits">throw and exception if not exists</param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> GetCustomerInfrastructure(ISession session, ICustomerPortalClient customerPortalClient, string customerInfrastructureName, bool throwIfNotExits)
        {
            CustomerInfrastructure currentCustomerInfrastructure = null;
            try
            {
                session.LogInformation($"Checking if exists the current Customer Infrastructure with name '{customerInfrastructureName}'.");
                currentCustomerInfrastructure = await customerPortalClient.GetObjectByName<CustomerInfrastructure>(customerInfrastructureName);
                session.LogInformation($"Customer Infrastructure with name '{customerInfrastructureName}' exists.");
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Cmf.Foundation.Common.CmfExceptionType.Db20001.ToString() && ex.Message.Contains("was not found in the system"))
            {
                // when was not found

                string errorMessage = $"The Customer Infrastructure with name '{customerInfrastructureName}' doesn't exists.";
                if (throwIfNotExits)
                {
                    session.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }

                session.LogInformation(errorMessage);
            }
            // other exceptions will throw!

            return currentCustomerInfrastructure;
        }

        /// <summary>
        /// Create a Customer Infrastructure without templates and parameters.
        /// This method does not wait for the cache to be updated and the infrastructure that is returned can have the object locked.
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="customer">Customer</param>
        /// <param name="customerInfrastructureName">Name of Customer Infrastructure</param>
        /// <returns></returns>
        public static Task<CustomerInfrastructure> CreateCustomerInfrastructure(ISession session, ProductCustomer customer, string customerInfrastructureName)
        {
            return CreateCustomerInfrastructure(session, customer, customerInfrastructureName, null, null);
        }

        /// <summary>
        /// Create a Customer Infrastructure.
        /// This method does not wait for the cache to be updated and the infrastructure that is returned can have the object locked.
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="customer">Customer</param>
        /// <param name="customerInfrastructureName">Name of Customer Infrastructure</param>
        /// <param name="parameters">Parameters of Customer Infrastructure</param>
        /// <param name="templates">Templates to Add to Customer Infrastructure</param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> CreateCustomerInfrastructure(ISession session, ProductCustomer customer, string customerInfrastructureName, string parameters, CustomerEnvironmentCollection templates)
        {
            session.LogInformation($"Creating Customer Infrastructure {customerInfrastructureName}...");

            // create infrastructure
            CustomerInfrastructure customerInfrastructure = new CustomerInfrastructure
            {
                Name = customerInfrastructureName,
                Customer = customer,
                Parameters = parameters,
            };

            customerInfrastructure = (await new CreateCustomerInfrastructureInput
            {
                CustomerInfrastructure = customerInfrastructure,
                TemplatesToAdd = templates
            }.CreateCustomerInfrastructureAsync(true)).CustomerInfrastructure;

            return customerInfrastructure;
        }

        /// <summary>
        /// Get the Customer Infrastructure URL
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="customerInfrastructure">Customer Infrastructure</param>
        /// <returns></returns>
        public static string GetInfrastructureUrl(ISession session, CustomerInfrastructure customerInfrastructure)
        {
            string infrastructureUrl = $"{(ClientConfigurationProvider.ClientConfiguration.UseSsl ? "https" : "http")}://{ClientConfigurationProvider.ClientConfiguration.HostAddress}/Entity/CustomerInfrastructure/{customerInfrastructure.Id}";
            session.LogInformation($"CustomerInfrastructure {customerInfrastructure.Name} accessible at {infrastructureUrl}");
            return infrastructureUrl;
        }

        /// <summary>
        /// Wait for CustomerInfrastructure to be unlocked. If timed out waiting, a exception will be throw.
        /// </summary>
        /// <param name="session">session</param>
        /// <param name="customerPortalClient">customer portal client</param>
        /// <param name="customerInfrastructure">customer infrastructure</param>
        /// <param name="secondsTimeout">seconds timeout to unlock the customer infrastructure</param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> WaitForCustomerInfrastructureUnlockAsync(ISession session, ICustomerPortalClient customerPortalClient, CustomerInfrastructure customerInfrastructure, int? secondsTimeout = _defaultSecondsTimeout)
        {
            bool failedUnlock = false;
            TimeSpan timeout = TimeSpan.FromSeconds(secondsTimeout.GetValueOrDefault(_defaultSecondsTimeout));
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                failedUnlock = await Task.Run(async () =>
                {
                    while (customerInfrastructure.ObjectLocked)
                    {
                        try
                        {
                            await Task.Delay(TimeSpan.FromSeconds(0.5), cancellationTokenSource.Token);

                            customerInfrastructure = await customerPortalClient.GetObjectByName<CustomerInfrastructure>(customerInfrastructure.Name, 1);
                        }
                        catch (TaskCanceledException)
                        {
                            return true;
                        }
                    }
                    return false;
                });
            }

            // if we failed to wait for CustomerInfrastructure to be unlocked, throw an error
            if (failedUnlock)
            {
                Exception error = new Exception("Timed out waiting for CustomerInfrastructure be Created and Unlocked.");
                session.LogError(error);
                throw error;
            }

            session.LogInformation($"CustomerInfrastructure {customerInfrastructure.Name} was Created and is Unlocked.");

            return customerInfrastructure;
        }
    }
}
