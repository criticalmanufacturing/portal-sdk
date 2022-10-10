using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class InfrastructureUtilities
    {
        /// <summary>
        /// Check if Customer Infrastructure already exists. 
        /// </summary>
        /// <param name="session">Session</param>
        /// <param name="customerPortalClient">Client to make requests to the Customer Portal's APIs</param>
        /// <param name="force">Force variable that indicates if the program can continue if the Customer INfrastructure already exists.</param>
        /// <param name="customerInfrastructureName">Name of Customer Infrastructure</param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> CheckCustomerInfrastructureAlreadyExists(ISession session, ICustomerPortalClient customerPortalClient, bool force, string customerInfrastructureName)
        {
            CustomerInfrastructure currentCustomerInfrastructure = null;
            try
            {
                session.LogInformation($"Checking if exists the current Customer Infrastructure name '{customerInfrastructureName}'.");
                currentCustomerInfrastructure = await customerPortalClient.GetObjectByName<CustomerInfrastructure>(customerInfrastructureName);

                if (!force)
                {
                    string errorMessage = $"The Customer Infrastructure with name '{customerInfrastructureName}' already exists and cannot be created. If you want to continue, please run with the 'Force' command.";
                    session.LogError(errorMessage);
                    throw new Exception(errorMessage);
                }
            }
            catch
            {
                // customerInfrastructure don't exists. Can continue to be created.
                session.LogInformation($"The Customer Infrastructure name '{customerInfrastructureName}' doesn't exists.");
            }

            return currentCustomerInfrastructure;
        }

        /// <summary>
        /// Create a Customer Infrastructure without templates and parameters
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
        /// Create a Customer Infrastructure
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
        /// <param name="session"></param>
        /// <param name="customerPortalClient"></param>
        /// <param name="customerInfrastructure"></param>
        /// <param name="secondsTimeout"></param>
        /// <returns></returns>
        public static async Task<CustomerInfrastructure> WaitForCustomerInfrastructureUnlockAsync(ISession session, ICustomerPortalClient customerPortalClient, CustomerInfrastructure customerInfrastructure, int? secondsTimeout = 180)
        {
            bool failedUnlock = false;
            TimeSpan timeout = TimeSpan.FromSeconds(secondsTimeout.Value);
            using (CancellationTokenSource cancellationTokenSource = new CancellationTokenSource(timeout))
            {
                failedUnlock = await Task.Run(async () =>
                {
                    while (!customerInfrastructure.ObjectLocked)
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
