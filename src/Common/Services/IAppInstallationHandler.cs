using Cmf.CustomerPortal.BusinessObjects;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IAppInstallationHandler
    {
        Task Handle(string appName, CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, string target, DirectoryInfo outputDir, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null);
    }
}
