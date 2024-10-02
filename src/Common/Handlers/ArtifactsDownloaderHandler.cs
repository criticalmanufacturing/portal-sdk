using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class DownloadArtifactsHandler : AbstractHandler
    {
        private readonly ISession _session;
        
        private readonly ICustomerPortalClient _customerPortalClient;

        private readonly IArtifactsDownloaderHandler _artifactsDownloaderHandler;

        public DownloadArtifactsHandler(ISession session, ICustomerPortalClient customerPortalClient,
            IArtifactsDownloaderHandler artifactsDownloaderHandler) : base(session, true)
        {
            _session = session;
            _customerPortalClient = customerPortalClient;
            _artifactsDownloaderHandler = artifactsDownloaderHandler;
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

            string outputPath = outputDir != null ? outputDir.FullName : Path.Combine(Directory.GetCurrentDirectory(), "out");

            // start download
            _session.LogInformation($"Downloading artifacts for Customer environment {customerEnvironmentName}...");
            await _artifactsDownloaderHandler.Handle(environment, outputPath);
            _session.LogInformation($"Artifacts successfully download to {outputPath}.");
        }
    }
}
