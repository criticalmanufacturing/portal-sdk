using System;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.Common.Licenses.Enums;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class UndeployEnvironmentHandler(
        ISession session,
        IEnvironmentUtilities newEnvironmentUtilities,
        ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
    {
        /// <summary>
        /// Notice logged when undeploying without purge, whose behaviour is about to change.
        /// </summary>
        public const string PurgeNotice = "Notice: undeploying without --purge currently also wipes out all the persistent data of the Customer Environment and runs its uninstall procedures. This will soon change to only remove its workloads, keeping its data. Use --purge to keep wiping out the Customer Environment completely.";

        /// <summary>
        /// Undeploys a Customer Environment: creates a new version of it and terminates the other versions,
        /// removing their deployments and volumes and running their uninstall procedures.
        /// </summary>
        /// <param name="name">The name of the Customer Environment to undeploy.</param>
        /// <param name="force">Skips the confirmation prompt.</param>
        /// <param name="purge">
        /// Wipes out the Customer Environment completely. Undeploying without it currently does the same,
        /// but logs <see cref="PurgeNotice"/> because it will soon only remove the workloads.
        /// </param>
        public async Task Run(string name, bool force, bool purge = false)
        {
            Session.LogInformation("The Undeploy operation will uninstall the Customer Environment cleaning up all persistent resources associated with it, rendering them unrecoverable.");

            if (!purge)
            {
                Session.LogInformation(PurgeNotice);
            }

            // Confirmation dialogue, skipped when --force is enabled
            if (!force)
            {
                Session.LogInformation("Do you wish to proceed? [y/N]");
                string? input = Console.ReadLine()?.Trim().ToLower();
                if (input != "y" && input != "yes")
                {
                    Session.LogInformation("Operation Cancelled.");
                    return;
                }
            }
            else
            {
                Session.LogInformation("Skipping confirmation because --force was specified.");
            }

            // login
            await EnsureLogin();

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

            if (environment.EnvironmentType == nameof(EnvironmentType.Production))
            {
                throw new InvalidOperationException("It is not possible to undeploy a Customer Environment of type 'Production'.");
            }

            // check environment connection
            await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

            Session.LogInformation($"Creating a new version of the Customer environment {name}...");
            environment = await customerEnvironmentServices.CreateEnvironment(environment);
            Session.LogInformation($"Customer environment {name} {environment.DescribeVersion()} created...");

            await customerEnvironmentServices.TerminateOtherVersions(environment, true, true, true);

            Session.LogInformation($"Customer environment {name} undeploy succeeded...");
        }
    }
}