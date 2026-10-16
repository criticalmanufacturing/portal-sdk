using Cmf.CustomerPortal.BusinessObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Gets the details of a Deployment Package.
/// </summary>
public class GetDeploymentPackageHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient) : AbstractHandler(session, true)
{
    /// <summary>
    /// Gets the details of a Deployment Package.
    /// </summary>
    /// <param name="name">Full name of the Deployment Package: its name and version, e.g. <c>@criticalmanufacturing\mes:12.0.0</c>.</param>
    /// <returns>The details of the Deployment Package.</returns>
    /// <exception cref="NotFoundException">The Deployment Package does not exist.</exception>
    public async Task<DeploymentPackageDetails> Run(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await EnsureLogin();

        Session.LogDebug($"Loading deployment package {name}");
        DeploymentPackage deploymentPackage;
        try
        {
            deploymentPackage = await customerPortalClient.GetObjectByName<DeploymentPackage>(name);
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            throw new NotFoundException(string.Format(Resources.DeploymentPackageNotFoundMessage, name));
        }

        return DeploymentPackageDetails.From(deploymentPackage);
    }
}
