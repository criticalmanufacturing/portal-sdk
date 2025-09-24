using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class InstallAppHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session,
        IFileSystem fileSystem,
        INewEnvironmentUtilities newEnvironmentUtilities,
        IAppInstallationHandler appInstallationHandler)
        : AbstractHandler(session, true)
    {
        public async Task Run(string name, string appVersion, string customerEnvironmentName, string license, FileInfo parameters, string[] replaceTokens, DirectoryInfo output, double? timeout, double? timeoutToGetSomeMBMessage = null)
        {
            // login
            await EnsureLogin();

            // replace tokens in the parameters file
            string appParameters = parameters == null ? null : await Utils.ReplaceTokens(Session, await fileSystem.File.ReadAllTextAsync(parameters.FullName), replaceTokens, true);

            // load the customer environment
            CustomerEnvironment environment = await customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);

            // check environment connection
            await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

            // create or update the relationship between the environment and the app
            CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage = await customerPortalClient.CreateOrUpdateAppInstallation(
                    environment.Id, name, appVersion, appParameters, license);

            // start deployment
            await appInstallationHandler.Handle(name, customerEnvironmentApplicationPackage, environment.DeploymentTarget, output, timeout, timeoutToGetSomeMBMessage);
        }
    }
}
