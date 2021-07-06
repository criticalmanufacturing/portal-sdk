using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class PublishPackageHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public PublishPackageHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        public async Task Run(FileSystemInfo path)
        {
            await EnsureLogin();

            // verify valid zip
            try
            {
                _ = System.IO.Compression.ZipFile.OpenRead(path.FullName).Entries;
            }
            catch
            {
                Session.LogError("Invalid package format. Package must be a valid zip package.");
                return;
            }

            // publish
            Session.LogDebug("Publishing package...");
            var publishNewNewStreamingOutput = await new PackageManagement.PublishApplicationPackageStreamingInput
            {
                FilePath = path.FullName
            }.PublishApplicationPackageAsync();

            Session.LogInformation("Package successfully uploaded");
        }
    }
}
