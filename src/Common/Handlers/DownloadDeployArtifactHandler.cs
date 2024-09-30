using Cmf.CustomerPortal.Sdk.Common.Services;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class ManifestsDownloaderHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        private readonly IManifestsDownloaderHandler _manifestsDownloaderHandler;

        public ManifestsDownloaderHandler(ICustomerPortalClient customerPortalClient, ISession session,
            IManifestsDownloaderHandler manifestsDownloaderHandler) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
            _manifestsDownloaderHandler = manifestsDownloaderHandler;
        }

        public async Task Run(string customerEnvironmentName, DirectoryInfo outputDir)
        {
            // login
            await EnsureLogin();

            // start download
            await _manifestsDownloaderHandler.Handle(customerEnvironmentName, outputDir);
        }
    }
}
