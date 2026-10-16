using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Cancels the in-progress deployment of an App in a Customer Environment.
/// </summary>
public class CancelAppDeploymentHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient,
    IEnvironmentUtilities environmentUtilities) : AbstractHandler(session, true)
{
    /// <summary>
    /// Cancels the deployment of the App named <paramref name="appName"/> in the Customer Environment named <paramref name="customerEnvironmentName"/>.
    /// </summary>
    /// <param name="appName">Name of the App.</param>
    /// <param name="customerEnvironmentName">Name of the Customer Environment where the App is installed.</param>
    /// <exception cref="ArgumentNullException">When a name is empty.</exception>
    /// <exception cref="Exception">When the Customer Environment does not exist or the App is not installed in it.</exception>
    public async Task Run(string appName, string customerEnvironmentName)
    {
        if (string.IsNullOrWhiteSpace(appName))
        {
            throw new ArgumentNullException(nameof(appName));
        }

        if (string.IsNullOrWhiteSpace(customerEnvironmentName))
        {
            throw new ArgumentNullException(nameof(customerEnvironmentName));
        }

        await EnsureLogin();

        Session.LogInformation($"Checking if app '{appName}' exists in customer environment '{customerEnvironmentName}'...");
        CustomerEnvironment? customerEnvironment;
        try
        {
            customerEnvironment = await environmentUtilities.GetLastNonNotDeployedCustomerEnvironmentVersion(customerEnvironmentName);
            if (customerEnvironment != null)
            {
                customerEnvironment = await customerPortalClient.GetCustomerEnvironmentById(customerEnvironment.Id, 1);
            }
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            customerEnvironment = null;
        }

        if (customerEnvironment == null)
        {
            throw new Exception($"Customer environment '{customerEnvironmentName}' was not found or has no deployed versions.");
        }

        CustomerEnvironmentApplicationPackage? customerEnvironmentApplicationPackage = null;
        if (customerEnvironment.RelationCollection != null
            && customerEnvironment.RelationCollection.TryGetValue("CustomerEnvironmentApplicationPackage", out var customerEnvironmentApplicationPackages))
        {
            customerEnvironmentApplicationPackage = customerEnvironmentApplicationPackages
                .OfType<CustomerEnvironmentApplicationPackage>()
                .FirstOrDefault(ceap => ceap.TargetEntity?.Name == appName);
        }

        if (customerEnvironmentApplicationPackage == null)
        {
            throw new Exception($"App '{appName}' is not installed in customer environment '{customerEnvironmentName}'.");
        }

        Session.LogInformation(
            $"Cancelling the deployment of app '{appName}' (Id {customerEnvironmentApplicationPackage.Id}) in customer environment '{customerEnvironmentName}' {customerEnvironment.DescribeVersion()}..."
        );
        await customerPortalClient.CancelAppDeployment(customerEnvironmentApplicationPackage.Id);

        Session.LogInformation($"App '{appName}' deployment cancel requested successfully.");
    }
}
