using Cmf.CustomerPortal.BusinessObjects;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IAppUninstallationHandler
    {
        Task Handle(CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage, double? timeoutMinutesMainTask = null, double? timeoutMinutesToGetSomeMBMsg = null);
    }
}
