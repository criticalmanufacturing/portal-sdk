using Cmf.CustomerPortal.BusinessObjects;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services
{
    /// <summary>
    /// Defines the interface for a singleton utilities class that defines helper methods to ease the creation of new Customer Environments.
    /// </summary>
    public interface INewEnvironmentUtilities
    {
        /// <summary>
        /// Transforms the input deployment target to the actual Customer Environment's DeploymentTarget.
        /// </summary>
        /// <param name="abbreviatedDeploymentTarget"></param>
        /// <returns></returns>
        string GetDeploymentTargetValue(DeploymentTarget abbreviatedDeploymentTarget);

        /// <summary>
        /// Gets all the other versions of the <paramref name="customerEnvironment"/> that are eligible to be terminated.
        /// </summary>
        /// <param name="customerEnvironment">Specific customer environment version</param>
        /// <returns></returns>
        Task<CustomerEnvironmentCollection> GetOtherVersionToTerminate(CustomerEnvironment customerEnvironment);


        /// <summary>
        /// Check the connection of an environment.
        /// If is a remote target and the deployment and the connection to the agent can't be startd, a exception is returned.
        /// </summary>
        /// <param name="environment">Environment</param>
        /// <exception cref="CmfFaultException">Throw an exception if the environment needs to have a connection established with the agent, and that is not possible.</exception>
        void CheckEnvironmentConnection(CustomerEnvironment environment);
    }
}
