using Cmf.CustomerPortal.BusinessObjects;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IEnvironmentDeploymentHandler
    {
        Task Handle(bool interactive, CustomerEnvironment customerEnvironment, DeploymentTarget deploymentTarget, DirectoryInfo outputDir, double? minutesTimeoutMainTask = null);

        Task WaitForEnvironmentsToBeTerminated(CustomerEnvironmentCollection customerEnvironments);
    }
}
