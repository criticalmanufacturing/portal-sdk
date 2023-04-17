using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class InstallAppHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;

        public InstallAppHandler(ICustomerPortalClient customerPortalClient, ISession session) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
        }

        public async Task Run(string name, string customerEnvironmentName, string license, FileInfo parameters, DirectoryInfo output)
        {
            await Task.CompletedTask;
        }
    }
}