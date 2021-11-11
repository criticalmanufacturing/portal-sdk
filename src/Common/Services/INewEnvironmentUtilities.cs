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
        string GetDeploymentTargetValue(string abbreviatedDeploymentTarget);

        /// <summary>
        /// Gets all the other versions of the <paramref name="customerEnvironment"/> that are eligible to be terminated.
        /// </summary>
        /// <param name="customerEnvironment">Specific customer environment version</param>
        /// <returns></returns>
        Task<CustomerEnvironmentCollection> GetOtherVersionToTerminate(CustomerEnvironment customerEnvironment);
    }
}
