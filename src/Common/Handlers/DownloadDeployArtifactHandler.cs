using Cmf.CustomerPortal.Sdk.Common.Services;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class DownloaderDeployArtifactHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        private readonly IArtifactDownloaderHandler _artifactDownloaderHandler;

        public DownloaderDeployArtifactHandler(ICustomerPortalClient customerPortalClient, ISession session,
                                               IArtifactDownloaderHandler artifactDownloaderHandler) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
            _artifactDownloaderHandler = artifactDownloaderHandler;
        }

        public async Task Run(string customerEnvironmentName, DirectoryInfo outputDir)
        {
            // login
            await EnsureLogin();

            // start download
            await _artifactDownloaderHandler.Handle(customerEnvironmentName, outputDir);
        }
    }
}
