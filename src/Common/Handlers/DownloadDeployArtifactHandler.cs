using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class ManifestsDownloaderHandler : AbstractHandler
    {
        private readonly ISession _session;
        
        private readonly ICustomerPortalClient _customerPortalClient;

        private readonly IManifestsDownloaderHandler _manifestsDownloaderHandler;

        public ManifestsDownloaderHandler(ICustomerPortalClient customerPortalClient, ISession session,
            IManifestsDownloaderHandler manifestsDownloaderHandler) : base(session, true)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _manifestsDownloaderHandler = manifestsDownloaderHandler;
        }

        public async Task Run(string customerEnvironmentName, DirectoryInfo outputDir)
        {
            // login
            await EnsureLogin();

            _session.LogInformation($"Checking if customer environment {customerEnvironmentName} exists...");
            // let's see if the environment already exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);
                _session.LogInformation($"Customer environment {customerEnvironmentName} actually exists...");
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
            {
                // when was not found
                string errorMessage = $"Customer environment {customerEnvironmentName} doesn't exist...";
                _session.LogInformation(errorMessage);

                throw new NotFoundException(errorMessage);
            }

            // start download
            _session.LogInformation($"Downloading manifests for Customer environment {customerEnvironmentName}...");
            await _manifestsDownloaderHandler.Handle(environment, outputDir);
            _session.LogInformation($"Manifests successfully download to {outputDir.FullName}.");
        }
    }
}
