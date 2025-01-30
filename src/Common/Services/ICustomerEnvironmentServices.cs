using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

public interface ICustomerEnvironmentServices
{
    /// <summary>
    /// Gets a customer environment.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="name">The customer environment name.</param>
    /// <returns>The customer environ</returns>
    Task<CustomerEnvironment> GetCustomerEnvironment(ISession session, string name);

    /// <summary>
    /// Creates the environment or a new version if it already exists.
    /// </summary>
    /// <param name="customerEnvironment">The customer environment.</param>
    /// <returns>The created environment.</returns>
    Task<CustomerEnvironment> CreateEnvironment(ICustomerPortalClient client, CustomerEnvironment customerEnvironment);

    /// <summary>
    /// Creates new customer environment entity or a new customer environment version.
    /// A new entity (entityType = Entity) of a customer environment makes sense when the customer environment doesn't exists and we want to create a new customer environment entity with the first version.
    /// To create a new customer environment version (entityType = Version) makes sense when the entity (with the first version) already exists.
    /// </summary>
    /// <param name="customerEnvironment">customer environment to create new entity</param>
    /// <param name="entityType">entityType for operation target (Entity -> for first version and entity creation | Version -> for a new version)</param>
    /// <returns>new customer environment with the first version</returns>
    Task<CustomerEnvironment> CreateNewEnvironmentEntityOrVersion(CustomerEnvironment customerEnvironment, EntityTypeSource entityType);

    /// <summary>
    /// Update a customer environment.
    /// </summary>
    /// <param name="customerEnvironment">customer environment</param>
    /// <returns></returns>
    Task<CustomerEnvironment> UpdateEnvironment(CustomerEnvironment customerEnvironment, CustomerEnvironmentDeploymentPackageCollection cedpCollection);
}
