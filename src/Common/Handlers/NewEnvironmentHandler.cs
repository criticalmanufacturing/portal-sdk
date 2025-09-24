using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.Common.Licenses.Enums;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironmentHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session,
        IFileSystem fileSystem,
        INewEnvironmentUtilities newEnvironmentUtilities,
        IEnvironmentDeploymentHandler environmentDeploymentHandler,
        ICustomerEnvironmentServices customerEnvironmentServices,
        ILicenseServices licenseService)
        : AbstractHandler(session, true)
    {
        public async Task Run(
            string name,
            FileInfo parameters,
            EnvironmentType environmentType,
            string siteName,
            string[] licensesNames,
            string deploymentPackageName,
            DeploymentTarget target,
            DirectoryInfo outputDir,
            string[] replaceTokens,
            bool interactive,
            string customerInfrastructureName,
            string description,
            bool terminateOtherVersions,
            bool isInfrastructureAgent,
            double? minutesTimeoutMainTask,
            double? minutesTimeoutToGetSomeMBMsg,
            bool terminateOtherVersionsRemove,
            bool terminateOtherVersionsRemoveVolumes
        )
        {
            // login
            await EnsureLogin();

            // build name and parameters if needed
            if (string.IsNullOrWhiteSpace(name))
            {
                // generate a short unique name based on
                var baselineTicks = new DateTime(2021, 1, 1).Ticks;
                var diffTicks = DateTime.Now.Ticks - baselineTicks;
                name = "env-" + diffTicks.ToString("x") + new Random().Next(0, 100);
            }

            string rawParameters = null;

            if (parameters != null)
            {
                rawParameters = await fileSystem.File.ReadAllTextAsync(parameters.FullName);
                rawParameters = await Utils.ReplaceTokens(Session, rawParameters, replaceTokens, true);
            }

            Session.LogInformation($"Checking if customer environment {name} exists...");

            // let's see if the environment already exists
            CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironment(Session, name);

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                if (description != null)
                {
                    environment.Description = description;
                }

                environment.DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target);
                environment.ChangeSet = null;

                // check environment connection
                await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

                Session.LogInformation($"Creating a new version of the Customer environment {name}...");
                environment = await customerEnvironmentServices.CreateEnvironment(customerPortalClient, environment);

                // Update environment with the parameters to be merged instead of overwriting
                environment.Parameters = rawParameters;
                environment = await customerEnvironmentServices.UpdateEnvironment(environment);

                // update Deployment Package
                if (!isInfrastructureAgent && !string.IsNullOrWhiteSpace(deploymentPackageName))
                {
                    // update Deployment Package
                    await customerEnvironmentServices.UpdateDeploymentPackage(
                        environment,
                        await customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                        [.. (await licenseService.GetLicensesByUniqueName(licensesNames)).Select(l => l.Id)]);
                }


                // terminate other versions
                if (terminateOtherVersions)
                {
                    Session.LogInformation("Terminating other versions...");

                    var customerEnvironmentsToTerminate = await newEnvironmentUtilities.GetOtherVersionToTerminate(environment);
                    OperationAttributeCollection terminateOperationAttibutes = new OperationAttributeCollection();
                    EntityType ceET = await _customerPortalClient.GetEntityTypeByName("CustomerEnvironment");
                    foreach (var ce in customerEnvironmentsToTerminate)
                    {
                        OperationAttribute attributeRemove = new OperationAttribute();
                        attributeRemove.EntityId = ce.Id;
                        attributeRemove.EntityType = ceET;
                        attributeRemove.Name = "RemoveDeployments";
                        attributeRemove.OperationName = "TerminateVersion";
                        attributeRemove.Value = terminateOtherVersionsRemove ? 1 : 0;

                        OperationAttribute attributeRemoveVolumes = new OperationAttribute();
                        attributeRemoveVolumes.EntityId = ce.Id;
                        attributeRemoveVolumes.EntityType = ceET;
                        attributeRemoveVolumes.Name = "RemoveVolumes";
                        attributeRemoveVolumes.OperationName = "TerminateVersion";
                        attributeRemoveVolumes.Value = (terminateOtherVersionsRemove && terminateOtherVersionsRemoveVolumes) ? 1 : 0;

                        terminateOperationAttibutes.Add(attributeRemove);
                        terminateOperationAttibutes.Add(attributeRemoveVolumes);
                    }
                    if (customerEnvironmentsToTerminate.Count > 0)
                    {
                        await customerPortalClient.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(customerEnvironmentsToTerminate, terminateOperationAttibutes);

                        // wait until they're terminated
                        List<long> ceTerminationFailedIds = await environmentDeploymentHandler.WaitForEnvironmentsToBeTerminated(customerEnvironmentsToTerminate);

                        if (ceTerminationFailedIds?.Count > 0)
                        {
                            string failedIdsString = string.Join(", ", ceTerminationFailedIds.Select(x => x.ToString()));
                            string errorMessage = $"Stopping deploy process because termination of other environment versions failed. Environment Ids of failed terminations: {failedIdsString}.";
                            Exception ex = new Exception(errorMessage);
                            Session.LogError(ex);

                            foreach (long ceId in ceTerminationFailedIds)
                            {
                                var logs = await _customerPortalClient.GetCustomerEnvironmentTerminationLogs(ceId);
                                Session.LogError($"\nCustomer Environment {ceId} did not terminate sucessully. Termination logs:\n {logs}\n");
                            }

                            throw ex;
                        }

                        Session.LogInformation("Other versions terminated!");
                    }
                    else
                    {
                        Session.LogInformation("There are no versions with an eligible status to be terminated.");
                    }
                }
            }
            // if not, check if we are creating a new environment for an infrastructure
            else if (!string.IsNullOrWhiteSpace(customerInfrastructureName))
            {
                ProductSite environmentSite = null;
                // If we are creating in an infrastructure, and we are not creating the agent, the user must define the site for the environment
                if (!isInfrastructureAgent)
                {
                    // If the user defined a site, load it
                    if (!string.IsNullOrEmpty(siteName))
                    {
                        environmentSite = await customerPortalClient.GetObjectByName<ProductSite>(siteName);
                    }
                    else
                    {
                        throw new ArgumentNullException(nameof(siteName), "Name of the Site is mandatory to create a Customer Environment");
                    }
                }

                environment = new CustomerEnvironment
                {
                    Name = name,
                    Description = description,
                    Parameters = rawParameters,
                    EnvironmentType = environmentType.ToString(),
                    DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Site = environmentSite,
                };

                // check environment connection
                await CheckConnectionNewEnvironmentCreation(environment, customerInfrastructureName);

                Session.LogInformation($"Creating the customer environment {name} for a customer infrastructure...");
                environment = await customerEnvironmentServices.CreateCustomerEnvironmentForCustomerInfrastructure(environment, customerInfrastructureName, isInfrastructureAgent);

                if (!isInfrastructureAgent)
                {
                    // update Deployment Package
                    await customerEnvironmentServices.UpdateDeploymentPackage(
                        environment,
                        await customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                        [.. (await licenseService.GetLicensesByUniqueName(licensesNames)).Select(l => l.Id)]);
                }
            }
            // if not, just create a new environment
            else
            {
                Session.LogInformation($"Creating customer environment {name}...");

                environment = new CustomerEnvironment
                {
                    EnvironmentType = environmentType.ToString(),
                    Site = isInfrastructureAgent ? null : await customerPortalClient.GetObjectByName<ProductSite>(siteName),
                    Name = name,
                    DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Parameters = rawParameters
                };

                environment = await customerEnvironmentServices.CreateEnvironment(customerPortalClient, environment);

                if (!isInfrastructureAgent)
                {
                    // update Deployment Package
                    await customerEnvironmentServices.UpdateDeploymentPackage(
                        environment,
                        await customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                        [.. (await licenseService.GetLicensesByUniqueName(licensesNames)).Select(l => l.Id)]);
                }
            }

            Session.LogInformation($"Customer environment {name} created...");

            // handle installation
            await environmentDeploymentHandler.Handle(interactive, environment, target, outputDir, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg);
        }

        /// <summary>
        /// Check the connection for a creation of a new environment in some infrastructure
        /// </summary>
        /// <param name="newEnvironment">The new environment</param>
        /// <param name="infrastructureName">Infrastructure name</param>
        private async Task CheckConnectionNewEnvironmentCreation(CustomerEnvironment newEnvironment, string infrastructureName)
        {
            CustomerInfrastructure infrastructure = new() { Name = infrastructureName };
            newEnvironment.CustomerInfrastructure = infrastructure;

            // check environment connection
            await newEnvironmentUtilities.CheckEnvironmentConnection(newEnvironment);
        }
    }
}
