using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface IAppUninstallationHandler
{
    Task Handle(
        CustomerEnvironmentApplicationPackage customerEnvironmentApplicationPackage,
        bool undeploy = false,
        double? timeoutMinutesMainTask = null,
        double? timeoutMinutesToGetSomeMBMsg = null
    );
}
