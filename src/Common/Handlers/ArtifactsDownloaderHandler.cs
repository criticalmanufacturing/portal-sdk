using System.IO;
using System.IO.Abstractions;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.Common;
using Cmf.LightBusinessObjects.Infrastructure.Errors;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class DownloadArtifactsHandler(
        ISession session,
        IFileSystem fileSystem,
        ICustomerPortalClient customerPortalClient,
        IArtifactsDownloaderHandler artifactsDownloaderHandler)
        : AbstractHandler(session, true)
    {
        private readonly ISession _session = session;

        public async Task Run(string customerEnvironmentName, DirectoryInfo outputDir)
        {
            // login
            await EnsureLogin();

            _session.LogInformation($"Checking if customer environment {customerEnvironmentName} exists...");
            // let's see if the environment already exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);
                _session.LogInformation($"Customer environment {customerEnvironmentName} actually exists...");
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == CmfExceptionType.Db20001.ToString())
            {
                // when was not found
                string errorMessage = $"Customer environment {customerEnvironmentName} doesn't exist...";
                _session.LogInformation(errorMessage);

                throw new NotFoundException(errorMessage);
            }

            string outputPath = outputDir != null
                ? outputDir.FullName
                : fileSystem.Path.Combine(fileSystem.Directory.GetCurrentDirectory(), "out");

            // start download
            _session.LogInformation($"Downloading artifacts for Customer environment {customerEnvironmentName}...");
            if (await artifactsDownloaderHandler.Handle(environment, outputPath))
            {
                _session.LogInformation($"Artifacts successfully downloaded to: {outputPath}");
            }
        }
    }
}
