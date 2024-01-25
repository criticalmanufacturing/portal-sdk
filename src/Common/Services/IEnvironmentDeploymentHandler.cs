using Cmf.CustomerPortal.BusinessObjects;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    public interface IEnvironmentDeploymentHandler
    {
        Task Handle(bool interactive, CustomerEnvironment customerEnvironment, DeploymentTarget deploymentTarget, DirectoryInfo outputDir, double? minutesTimeoutMainTask = null, double? minutesTimeoutToGetSomeMBMsg = null);

        /// <summary>
        /// Wait for the customer environments to finish termination (successfully or not) and return the ids of those that failed
        /// </summary>
        /// <param name="customerEnvironments">customer environments to wait termination</param>
        /// <returns>List of ids of the customer environments marked with TerminationFailed</returns>
        Task<List<long>> WaitForEnvironmentsToBeTerminated(CustomerEnvironmentCollection customerEnvironments);
    }
}
