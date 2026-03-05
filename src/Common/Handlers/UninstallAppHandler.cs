using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;
namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class UninstallAppHandler(
        ISession session,
        ICustomerPortalClient customerPortalClient) : AbstractHandler(session, true)
    {
        public async Task Run(string appName, string customerEnvironmentName, bool terminateOtherVersionsRemove,
            bool terminateOtherVersionsRemoveVolumes)
        {
            Session.LogInformation($"Starting uninstall operation for application '{appName}' in customer environment '{customerEnvironmentName}'.");
            await EnsureLogin();
            CustomerEnvironment? customerEnvironment = null;
            try
            {
                customerEnvironment = await customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironmentName);
                customerEnvironment = await customerPortalClient.GetCustomerEnvironmentById(customerEnvironment.Id, 1);
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
            {
                Session.LogError($"Customer environment '{customerEnvironmentName}' was not found. Aborting uninstall.");
                return;
            }

            if (customerEnvironment.UniversalState == Foundation.Common.Base.UniversalState.Terminated)
            {
                Session.LogError($"Customer environment '{customerEnvironmentName}' is terminated; uninstall cannot proceed.");
                return;
            }
            if (customerEnvironment.Status == DeploymentStatus.NotDeployed)
            {
                Session.LogError($"Customer environment '{customerEnvironmentName}' is not deployed; nothing to uninstall.");
                return;
            }

            if (customerEnvironment.RelationCollection != null
                && customerEnvironment.RelationCollection.TryGetValue("CustomerEnvironmentApplicationPackage", out var customerEnvironmentApplicationPackages))
            {
                CustomerEnvironmentApplicationPackage? customerEnvironmentApplicationPackage = null;

                foreach (var item in customerEnvironmentApplicationPackages)
                {
                    if (item is CustomerEnvironmentApplicationPackage ceap && ceap.TargetEntity?.Name == appName)
                    {
                        Session.LogInformation($"Application '{appName}' found in environment '{customerEnvironmentName}' (relation id: {ceap.Id}).");
                        customerEnvironmentApplicationPackage = ceap;
                        break;
                    }
                }

                if (customerEnvironmentApplicationPackage == null)
                {
                    Session.LogError($"Application '{appName}' is not installed in customer environment '{customerEnvironmentName}'. Aborting uninstall.");
                    return;
                }

                await customerPortalClient.StartAppUninstall(customerEnvironmentApplicationPackage.Id, terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);
                Session.LogInformation($"Uninstall request submitted for application '{appName}' (relation id: {customerEnvironmentApplicationPackage.Id}) in environment '{customerEnvironmentName}'.");
            }
            else
            {
                Session.LogError($"No installed applications found for customer environment '{customerEnvironmentName}'. Application '{appName}' is not installed.");
            }
        }
    }
}