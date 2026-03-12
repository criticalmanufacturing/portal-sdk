using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IAppUninstallationHandler
    {
        Task Handle(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, bool removeDeployments = false, bool removeVolumes = false, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null);
    }
}
