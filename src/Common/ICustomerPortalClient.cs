using Cmf.CustomerPortal.BusinessObjects;
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
        Task<IMessageBusTransport> GetMessageBusTransport();

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
        /// <param name="softwareLicenseName">Name of a SoftwareLicense.</param>
        /// <returns>The CustomerEnvironmentApplicationPackage relation.</returns>
        Task<CustomerEnvironmentApplicationPackage> CreateOrUpdateAppInstallation(long customerEnvironmentId, string appName, string appVersion, string parameters, string softwareLicenseName);

        /// <summary>
        /// Check the Deployment connection to verify if the deployment of an environment/app can occur.
        /// </summary>
        /// <param name="customerEnvironment">customer environment</param>
        /// <param name="customerInfrastructure">customer infrastructure</param>
        /// <returns></returns>
        Task<bool> CheckStartDeploymentConnection(CustomerEnvironment customerEnvironment, CustomerInfrastructure customerInfrastructure);

        /// <summary>
        /// Loads the attachments that a certain entity has.
        /// </summary>
        /// <param name="entityBase">Entity instance.</param>
        /// <returns>A collection of entity documentation instances.</returns>
        Task<EntityDocumentationCollection> GetAttachmentsForEntity(EntityBase entityBase);

        /// <summary>
        /// Downloads a certain attachment, by id.
        /// </summary>
        /// <param name="attachmentId">Id of an EntityDocumentation to download.</param>
        /// <returns>The attachment file path.</returns>
        Task<string> DownloadAttachmentStreaming(long attachmentId);

        /// <summary>
        /// Loads the termination logs for a particular customer environment.
        /// </summary>
        /// <param name="ceId">Id of the Customer Environment.</param>
        /// <returns></returns>
        Task<string> GetCustomerEnvironmentTerminationLogs(long ceId);

        /// <summary>
        /// Loads an entity type by its name.
        /// </summary>
        /// <param name="name">Name of the entity type.</param>
        /// <returns></returns>
        Task<EntityType> GetEntityTypeByName(string name);

        /// <summary>
        /// Update a customer environment.
        /// </summary>
        /// <param name="customerEnvironment">customer environment</param>
        /// <returns></returns>
        public Task<CustomerEnvironment> UpdateEnvironment(CustomerEnvironment customerEnvironment);

        /// <summary>
        /// Get Customer Environment By Id
        /// </summary>
        /// <param name="customerEnvironmentId">customer Environment id</param>
        /// <returns></returns>
        public Task<CustomerEnvironment> GetCustomerEnvironmentById(long customerEnvironmentId, int levelsToLoad = 0);

        /// <summary>
        /// Starts the uninstallation of an application, given its id and the options to remove deployments and volumes.
        /// </summary>
        /// <param name="appId"></param>
        /// <param name="removeDeployments"></param>
        /// <param name="removeVolumes"></param>
        /// <returns></returns>
        public Task StartAppUninstall(long appId, bool removeDeployments, bool removeVolumes, bool undeploy);

        /// <summary>
        /// Gets the infrastructure agent related to a customer environment, given the environment's name.
        /// </summary>
        /// <param name="customerEnvironmentName">Name of the customer environment.</param>
        /// <returns>The infrastructure agent associated with the specified customer environment.</returns>
        public Task<CustomerEnvironment> GetCustomerInfrastructureAgentByCustomerEnvironment(string customerEnvironmentName);

        /// <summary>
        /// Terminates a list of customer environments, given their ids and the operation attributes to be passed to the service
        ///     to customize terminate behaviors, such as deleting workloads, volumes or performing an undeploy.
        /// </summary>
        /// <param name="customerEnvironmentIds"></param>
        /// <param name="operationAttributes"></param>
        /// <returns></returns>
        public Task TerminateCustomerEnvironments(List<long> customerEnvironmentIds, OperationAttributeCollection operationAttributes);
    }
}