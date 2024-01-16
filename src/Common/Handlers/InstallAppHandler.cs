using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class InstallAppHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        private readonly INewEnvironmentUtilities _newEnvironmentUtilities;

        private readonly IAppInstallationHandler _appInstallationHandler;

        public InstallAppHandler(ICustomerPortalClient customerPortalClient, ISession session,
            INewEnvironmentUtilities newEnvironmentUtilities, IAppInstallationHandler appInstallationHandler) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
            _newEnvironmentUtilities = newEnvironmentUtilities;
            _appInstallationHandler = appInstallationHandler;
        }

        public async Task Run(string name, string appVersion, string customerEnvironmentName, string license, FileInfo parameters, string[] replaceTokens, DirectoryInfo output, double? timeout, double? timeoutToGetSomeMBMessage = null)
        {
            // login
            await EnsureLogin();

            // replace tokens in the parameters file
            string appParameters = parameters == null ? null : await Utils.ReplaceTokens(Session, File.ReadAllText(parameters.FullName), replaceTokens, true);

            // load the customer environment
            CustomerEnvironment environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);

            // check environment connection
            _newEnvironmentUtilities.CheckEnvironmentConnection(environment);

            // create or update the relationship between the environment and the app
            CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage = await _customerPortalClient.CreateOrUpdateAppInstallation(
                    environment.Id, name, appVersion, appParameters, license);

            // start deployment
            await _appInstallationHandler.Handle(name, customerEnvironmentApplicationPackage, environment.DeploymentTarget, output, timeout, timeoutToGetSomeMBMessage);
        }
    }
}