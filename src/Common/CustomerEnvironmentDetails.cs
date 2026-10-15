using Cmf.CustomerPortal.BusinessObjects;
using System;

namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// The details of a Customer Environment version that are shown to the user.
/// </summary>
/// <param name="Id">Id of the version.</param>
/// <param name="Name">Name of the Customer Environment.</param>
/// <param name="Version">Version number of the version.</param>
/// <param name="Description">Description of the Customer Environment.</param>
/// <param name="EnvironmentType">Type of the Customer Environment, e.g. Development or Production.</param>
/// <param name="Status">Deployment status of the version.</param>
/// <param name="UniversalState">Universal state of the version, e.g. Effective or Terminated.</param>
/// <param name="Site">Name of the Site associated with the Customer Environment.</param>
/// <param name="CustomerInfrastructure">Name of the Customer Infrastructure the Customer Environment belongs to.</param>
/// <param name="DeploymentPackage">Name of the Deployment Package of the version.</param>
/// <param name="DeploymentTarget">Deployment Target of the version.</param>
/// <param name="CreatedBy">User that created the version.</param>
/// <param name="CreatedOn">When the version was created.</param>
/// <param name="ModifiedBy">User that last modified the version.</param>
/// <param name="ModifiedOn">When the version was last modified.</param>
public sealed record CustomerEnvironmentDetails(
    long Id,
    string Name,
    int Version,
    string? Description,
    string? EnvironmentType,
    string Status,
    string UniversalState,
    string? Site,
    string? CustomerInfrastructure,
    string? DeploymentPackage,
    string? DeploymentTarget,
    string? CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime ModifiedOn)
{
    /// <summary>
    /// Creates the details of <paramref name="environment"/>.
    /// </summary>
    /// <param name="environment">The Customer Environment version.</param>
    /// <returns>The details of the version.</returns>
    public static CustomerEnvironmentDetails From(CustomerEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(environment);

        return new CustomerEnvironmentDetails(
            environment.Id,
            environment.Name,
            environment.Version,
            NullIfEmpty(environment.Description),
            NullIfEmpty(environment.EnvironmentType),
            environment.Status.ToString(),
            environment.UniversalState.ToString(),
            NullIfEmpty(environment.Site?.Name),
            NullIfEmpty(environment.CustomerInfrastructure?.Name),
            NullIfEmpty(environment.DeploymentPackage?.Name),
            NullIfEmpty(environment.DeploymentTarget),
            NullIfEmpty(environment.CreatedBy),
            environment.CreatedOn,
            NullIfEmpty(environment.ModifiedBy),
            environment.ModifiedOn);
    }

    private static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
}
