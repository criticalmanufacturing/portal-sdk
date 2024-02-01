using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.OutputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.Common.Base;
using Cmf.Foundation.Security;
using Cmf.MessageBus.Client;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common
{
    /// <summary>
    /// Defines the interface of a client that is responsible to the requests to the Customer Portal's APIs.
    /// </summary>
    public interface ICustomerPortalClient
    {
        /// <summary>
        /// Get's the MessageBus transport's configurations.
        /// </summary>
        /// <returns><see cref="Transport"/> configured</returns>
        Task<Transport> GetMessageBusTransport();

        /// <summary>
        /// Get's an object by its name.
        /// </summary>
        /// <typeparam name="T">The object's Type</typeparam>
        /// <param name="name">Name of the object</param>
        /// <param name="levelsToLoad">Levels to load, defaulting to 0. More levels means more information and, therefore, a bigger payload.</param>
        /// <returns>The loaded object</returns>
        Task<T> GetObjectByName<T>(string name, int levelsToLoad = 0) where T : CoreBase, new();

        /// <summary>
        /// Loads the EntityRelations for an object.
        /// </summary>
        /// <typeparam name="T">The object's Type</typeparam>
        /// <param name="obj">The actual object</param>
        /// <param name="relationsNames">Names of the relations to be loaded</param>
        /// <returns>The <paramref name="obj"/> with the RelationCollection property loaded</returns>
        Task<T> LoadObjectRelations<T>(T obj, Collection<string> relationsNames) where T : CoreBase, new();

        /// <summary>
        /// Executes a query and returns its data in a <see cref="DataSet"/> format.
        /// </summary>
        /// <param name="queryObject">QueryObject pre-configured</param>
        /// <returns><see cref="DataSet"/> filled with the results</returns>
        Task<DataSet> ExecuteQuery(QueryObject queryObject);

        /// <summary>
        /// Terminates a series of objects.
        /// </summary>
        /// <typeparam name="T">The type of the objects' list. Must inherit a <see cref="List{U}"/>.</typeparam>
        /// <typeparam name="U">The objects' type</typeparam>
        /// <param name="obj">The list of objects</param>
        /// <param name="operationAttributes">Operation attributes to pass to the service</param>
        /// <returns>The same list of objects</returns>
        Task<T> TerminateObjects<T, U>(T obj, OperationAttributeCollection operationAttributes = null, bool isToTerminateAllVersions = false) where T : List<U>, new() where U : new();

        /// <summary>
        /// Gets a collection of CustomerEnvironments.
        /// </summary>
        /// <param name="ids">Ids of the CustomerEnvironments</param>
        /// <returns>The collection of CustomerEnvironments with some properties filled.</returns>
        Task<CustomerEnvironmentCollection> GetCustomerEnvironmentsById(long[] ids);

        /// <summary>
        /// Check if Customer Environment is connected
        /// </summary>
        /// <param name="definitionId">definition id</param>
        /// <returns></returns>
        Task<bool> CheckCustomerEnvironmentConnectionStatus(long? definitionId);

        /// Get's an object by its Id.
        /// </summary>
        /// <typeparam name="T">The object's Type</typeparam>
        /// <param name="name">Name of the object</param>
        /// <param name="levelsToLoad">Levels to load, defaulting to 0. More levels means more information and, therefore, a bigger payload.</param>
        /// <returns>The loaded object</returns>
        Task<T> GetObjectById<T>(long id, int levelsToLoad = 0) where T : CoreBase, new();
        /// <summary>
        /// Get current user authenticated
        /// </summary>
        /// <returns>Current user</returns>
        Task<User> GetCurrentUser();

        /// <summary>
        /// Creates or updates the relationship between an Application Package and a Customer Environment.
        /// </summary>
        /// <param name="customerEnvironmentId">Id of the CustomerEnvironments.</param>
        /// <param name="appName">Name of the ApplicationPackage.</param>
        /// <param name="appVersion">Version of the ApplicationPackage.</param>
        /// <param name="parameters">Deployment parameters for the CustomerEnvironmentApplicationPackage.</param>
        /// <param name="customerLicenseName">Name of a CustomerLicense.</param>
        /// <returns>The CustomerEnvironmentApplicationPackage relation.</returns>
        Task<CustomerEnvironmentApplicationPackage> CreateOrUpdateAppInstallation(long customerEnvironmentId, string appName, string appVersion, string parameters, string customerLicenseName);

        /// <summary>
        /// Check the Deployment connection to verify if the deployment of an environment/app can occur.
        /// </summary>
        /// <param name="customerEnvironment">customer environment</param>
        /// <param name="customerInfrastructure">customer infrastructure</param>
        /// <returns></returns>
        Task<CheckStartDeploymentConnectionOutput> CheckStartDeploymentConnection(CustomerEnvironment customerEnvironment, CustomerInfrastructure customerInfrastructure);
    }
}