using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Gets the details of a Customer Environment version.
/// </summary>
public class GetEnvironmentHandler(
    ISession session,
    ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
{
    /// <summary>
    /// Gets the details of a Customer Environment version.
    /// </summary>
    /// <param name="name">Name of the Customer Environment.</param>
    /// <param name="version">Version number of the version, or <c>null</c> for the current version.</param>
    /// <returns>The details of the version.</returns>
    /// <exception cref="NotFoundException">The Customer Environment, or the requested version, does not exist.</exception>
    public async Task<CustomerEnvironmentDetails> Run(string name, int? version)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await EnsureLogin();

        CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironmentVersion(name, version);
        return CustomerEnvironmentDetails.From(environment);
    }
}
