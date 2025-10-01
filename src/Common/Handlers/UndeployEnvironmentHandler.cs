using System;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class UndeployEnvironmentHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session,
        INewEnvironmentUtilities newEnvironmentUtilities,
        IEnvironmentDeploymentHandler environmentDeploymentHandler,
        ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
    {
        // private readonly ICustomerPortalClient _customerPortalClient;
        // private readonly INewEnvironmentUtilities _newEnvironmentUtilities;
        // private readonly IEnvironmentDeploymentHandler _environmentDeploymentHandler;
        // private readonly ICustomerEnvironmentServices _customerEnvironmentServices;

        // public UndeployEnvironmentHandler(ICustomerPortalClient customerPortalClient, ISession session,
        //     INewEnvironmentUtilities newEnvironmentUtilities, IEnvironmentDeploymentHandler environmentDeploymentHandler,
        //     ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
        // {
        //     // _customerPortalClient = customerPortalClient;
        //     // _newEnvironmentUtilities = newEnvironmentUtilities;
        //     // _environmentDeploymentHandler = environmentDeploymentHandler;
        //     // _customerEnvironmentServices = customerEnvironmentServices;
        // }

        public async Task Run(string name, bool terminateOtherVersionsRemoveVolumes)
        {
            // login
            await EnsureLogin();

            // check name
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentNullException(nameof(name));
            }

            Session.LogInformation($"Checking if customer environment {name} exists...");

            // check if the environment exists, this is mandatory for the operation to continue
            CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironment(Session, name);

            // if the customer environment does not exist, throw an exception
            if (environment == null)
            {
                throw new Exception($"Customer environment with name '{name}' does not exist...");
            }

            // check environment connection
            await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

            Session.LogInformation($"Creating a new version of the Customer environment {name}...");
            environment = await customerEnvironmentServices.CreateEnvironment(customerPortalClient, environment);

            await customerEnvironmentServices.TerminateOtherVersions(Session, newEnvironmentUtilities, customerPortalClient, environmentDeploymentHandler, environment, true, terminateOtherVersionsRemoveVolumes);

            Session.LogInformation($"Customer environment {name} undeploy succeeded...");
        }
    }
}