using System;
using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;

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

        public async Task Run(string name, string customerEnvironmentName, string license, FileInfo parameters, string[] replaceTokens, DirectoryInfo output, double? timeout)
        {
            // login
            await EnsureLogin();

            // replace tokens in the parameters file
            string appParameters = parameters == null ? null : await Utils.ReplaceTokens(Session, File.ReadAllText(parameters.FullName), replaceTokens, true);

            // load the customer environment
            CustomerEnvironment environment = null;
            environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);

            // create or update the relationship between the environment and the app
            CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage = null;
            customerEnvironmentApplicationPackage = await _customerPortalClient.CreateOrUpdateAppInstallation(
                environment.Id, name, appParameters, license
            );

            // start deployment
            await _appInstallationHandler.Handle(name, customerEnvironmentApplicationPackage, environment.DeploymentTarget, output, timeout);
        }
    }
}