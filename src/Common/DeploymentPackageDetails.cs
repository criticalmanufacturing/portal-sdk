using Cmf.CustomerPortal.BusinessObjects;
using System;

namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// The details of a Deployment Package that are shown to the user.
/// </summary>
/// <param name="Id">Id of the Deployment Package.</param>
/// <param name="Name">Full name of the Deployment Package: its name and version, e.g. <c>@criticalmanufacturing\mes:12.0.0</c>.</param>
/// <param name="DisplayName">Display name of the Deployment Package.</param>
/// <param name="Description">Description of the Deployment Package.</param>
/// <param name="PackageVersion">Version of the package.</param>
/// <param name="UniversalState">Universal state of the Deployment Package, e.g. Effective or Terminated.</param>
/// <param name="IsRootPackage">Whether the Deployment Package is a root package.</param>
/// <param name="IsInstallable">Whether the Deployment Package can be installed.</param>
/// <param name="PreRelease">Whether the Deployment Package is a pre-release.</param>
/// <param name="ManifestVersion">Version of the manifest of the Deployment Package.</param>
/// <param name="LicenseVersion">License version of the Deployment Package.</param>
/// <param name="DocumentationUrl">URL of the documentation of the Deployment Package.</param>
/// <param name="CreatedBy">User that created the Deployment Package.</param>
/// <param name="CreatedOn">When the Deployment Package was created.</param>
/// <param name="ModifiedBy">User that last modified the Deployment Package.</param>
/// <param name="ModifiedOn">When the Deployment Package was last modified.</param>
/// <param name="Manifest">Manifest XML of the Deployment Package.</param>
public sealed record DeploymentPackageDetails(
    long Id,
    string Name,
    string? DisplayName,
    string? Description,
    string? PackageVersion,
    string UniversalState,
    bool? IsRootPackage,
    bool? IsInstallable,
    bool? PreRelease,
    string? ManifestVersion,
    string? LicenseVersion,
    string? DocumentationUrl,
    string? CreatedBy,
    DateTime CreatedOn,
    string? ModifiedBy,
    DateTime ModifiedOn,
    string? Manifest)
{
    /// <summary>
    /// Creates the details of <paramref name="deploymentPackage"/>.
    /// </summary>
    /// <param name="deploymentPackage">The Deployment Package.</param>
    /// <returns>The details of the Deployment Package.</returns>
    public static DeploymentPackageDetails From(DeploymentPackage deploymentPackage)
    {
        ArgumentNullException.ThrowIfNull(deploymentPackage);

        return new DeploymentPackageDetails(
            deploymentPackage.Id,
            deploymentPackage.Name,
            Utils.NullIfEmpty(deploymentPackage.DisplayName),
            Utils.NullIfEmpty(deploymentPackage.Description),
            Utils.NullIfEmpty(deploymentPackage.PackageVersion),
            deploymentPackage.UniversalState.ToString(),
            deploymentPackage.IsRootPackage,
            deploymentPackage.IsInstallable,
            deploymentPackage.PreRelease,
            Utils.NullIfEmpty(deploymentPackage.ManifestVersion),
            Utils.NullIfEmpty(deploymentPackage.LicenseVersion),
            Utils.NullIfEmpty(deploymentPackage.DocumentationURL),
            Utils.NullIfEmpty(deploymentPackage.CreatedBy),
            deploymentPackage.CreatedOn,
            Utils.NullIfEmpty(deploymentPackage.ModifiedBy),
            deploymentPackage.ModifiedOn,
            Utils.NullIfEmpty(deploymentPackage.Manifest));
    }
}
