using System;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class UndeployEnvironmentHandler(
        ISession session,
        INewEnvironmentUtilities newEnvironmentUtilities,
        ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
    {
        public async Task Run(string name, bool force)
        {
            Session.LogInformation("The Undeploy operation will uninstall the Customer Environment cleaning up all persistent resources associated with it, rendering them unrecoverable.");

            // Confirmation dialogue, skipped when --force is enabled
            if (!force) 
            {
                Console.WriteLine("Do you wish to proceed? [y/n]");
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (input != "y" && input != "yes")
                {
                    Console.WriteLine("Operation Cancelled.");
                    return;
                }
            } else
            {
                Session.LogInformation("Skipping confirmation dialogue with --force specified.");
            }

                // login
                await EnsureLogin();

            // Feature preview notice
            Session.LogInformation("[Preview] The 'undeploy' feature is currently in feature preview and may change.");

            // check name
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Session.LogInformation($"Checking if customer environment {name} exists...");

            // check if the environment exists, this is mandatory for the operation to continue
            CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironment(name);

            // if the customer environment does not exist, throw an exception
            if (environment == null)
            {
                throw new Exception($"Customer environment with name '{name}' does not exist...");
            }

            // check environment connection
            await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

            Session.LogInformation($"Creating a new version of the Customer environment {name}...");
            environment = await customerEnvironmentServices.CreateEnvironment(environment);

            await customerEnvironmentServices.TerminateOtherVersions(environment, true, true, true);

            Session.LogInformation($"Customer environment {name} undeploy succeeded...");
        }
    }
}