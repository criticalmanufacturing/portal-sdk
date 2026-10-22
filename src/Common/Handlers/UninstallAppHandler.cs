using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

public class UninstallAppHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient,
    IEnvironmentUtilities environmentUtilities,
    IAppUninstallationHandler appUninstallationHandler
) : AbstractHandler(session, true)
{
    /// <summary>
    /// Uninstalls an App from the latest deployed version of a Customer Environment.
    /// By default only the App's workloads are removed.
    /// </summary>
    /// <param name="appName">The name of the App to uninstall.</param>
    /// <param name="customerEnvironmentName">The name of the Customer Environment the App is installed on.</param>
    /// <param name="removeVolumes">Also removes the App's volumes.</param>
    /// <param name="undeploy">Also runs the App's undeploy procedures, which implies removing its volumes.</param>
    /// <param name="timeout">Timeout, in minutes, to wait for the uninstall to finish.</param>
    /// <param name="timeoutToGetSomeMBMessage">Timeout, in minutes, to wait for any Message Bus message from the portal.</param>
    /// <param name="purge">Wipes out the App completely: removes its volumes and runs its undeploy procedures.</param>
    public async Task Run(
        string appName,
        string customerEnvironmentName,
        bool removeVolumes,
        bool undeploy,
        double? timeout,
        double? timeoutToGetSomeMBMessage = null,
        bool purge = false
    )
    {
        Session.LogInformation(
            $"Starting uninstall operation for application '{appName}' in customer environment '{customerEnvironmentName}'."
        );
        await EnsureLogin();
        CustomerEnvironment? customerEnvironment = null;
        try
        {
            customerEnvironment =
                await environmentUtilities.GetLastNonNotDeployedCustomerEnvironmentVersion(customerEnvironmentName);
            if (customerEnvironment == null)
            {
                Session.LogError(
                    $"Customer Environment '{customerEnvironmentName}' was not found or has no deployed versions. Aborting uninstall."
                );
                return;
            }

            customerEnvironment = await customerPortalClient.GetCustomerEnvironmentById(customerEnvironment.Id, 1);
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            Session.LogError($"Customer environment '{customerEnvironmentName}' was not found. Aborting uninstall.");
            return;
        }

        if (customerEnvironment.UniversalState == Foundation.Common.Base.UniversalState.Terminated)
        {
            Session.LogError(
                $"Customer environment '{customerEnvironmentName}' is terminated; uninstall cannot proceed."
            );
            return;
        }

        if (customerEnvironment.Status == DeploymentStatus.NotDeployed)
        {
            Session.LogError(
                $"Customer environment '{customerEnvironmentName}' is not deployed; nothing to uninstall."
            );
            return;
        }

        if (customerEnvironment.RelationCollection == null || !customerEnvironment.RelationCollection.TryGetValue(
                "CustomerEnvironmentApplicationPackage",
                out var customerEnvironmentApplicationPackages
            )
           )
        {
            Session.LogError(
                $"App '{appName}' is not installed in the latest deployed version of the customer environment '{customerEnvironmentName}'."
            );
            return;
        }

        CustomerEnvironmentApplicationPackage? customerEnvironmentApplicationPackage = null;

        foreach (var item in customerEnvironmentApplicationPackages)
        {
            if (item is CustomerEnvironmentApplicationPackage ceap && ceap.TargetEntity?.Name == appName)
            {
                Session.LogInformation(
                    $"App '{appName}' (Id {ceap.Id}) found in customer environment '{customerEnvironmentName}' {customerEnvironment.DescribeVersion()}."
                );
                customerEnvironmentApplicationPackage = ceap;
                break;
            }
        }

        if (customerEnvironmentApplicationPackage == null)
        {
            Session.LogError(
                $"Application '{appName}' is not installed in customer environment '{customerEnvironmentName}'. Aborting uninstall."
            );
            return;
        }

        if (purge)
        {
            Session.LogInformation(
                $"Purging App '{appName}' (Id {customerEnvironmentApplicationPackage.Id}): its volumes will be removed and its undeploy procedures executed."
            );
        }

        await appUninstallationHandler.Handle(
            customerEnvironmentApplicationPackage,
            removeVolumes || purge,
            undeploy || purge,
            timeout,
            timeoutToGetSomeMBMessage
        );
    }
}
