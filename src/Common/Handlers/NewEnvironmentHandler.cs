using System.IO.Abstractions;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.Foundation.Common.Base;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironmentHandler(
        ICustomerPortalClient customerPortalClient,
        ISession session,
        IFileSystem fileSystem,
        IEnvironmentUtilities newEnvironmentUtilities,
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
            DeploymentTarget? target,
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
            CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironment(name);

            // Resolve licenses before creating a version or changing the environment. Creation/update
            // responses may not include the existing deployment package relations.
            long[] softwareLicenseIds = null;
            if (!isInfrastructureAgent && !string.IsNullOrWhiteSpace(deploymentPackageName))
            {
                softwareLicenseIds = await ResolveSoftwareLicenseIds(environment, licensesNames);
            }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                if (description != null)
                {
                    environment.Description = description;
                }

                if (target.HasValue)
                {
                    environment.DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target.Value);
                }
                environment.ChangeSet = null;

                // check environment connection
                await newEnvironmentUtilities.CheckEnvironmentConnection(environment);

                // create a new CE version if the latest isn't Created or NotDeployed
                if (environment.UniversalState != UniversalState.Created || environment.Status != DeploymentStatus.NotDeployed)
                {
                    Session.LogInformation($"Creating a new version of the Customer environment {name}...");
                    environment = await customerEnvironmentServices.CreateEnvironment(environment);
                }

                // Update environment with the parameters to be merged instead of overwriting
                environment.Parameters = rawParameters;
                environment = await customerEnvironmentServices.UpdateEnvironment(environment);

                // update Deployment Package
                if (!isInfrastructureAgent && !string.IsNullOrWhiteSpace(deploymentPackageName))
                {
                    await UpdateDeploymentPackage(environment, deploymentPackageName, softwareLicenseIds);
                }


                // terminate other versions
                if (terminateOtherVersions)
                {
                    await customerEnvironmentServices.TerminateOtherVersions(environment, terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes, false);
                }
            }
            // if not, check if we are creating a new environment for an infrastructure
            else if (!string.IsNullOrWhiteSpace(customerInfrastructureName))
            {
                // if we are creating an environment, we need to validate that some options are provided
                ValidateCustomerEnvironmentArgs(target, siteName, deploymentPackageName, isInfrastructureAgent);

                ProductSite environmentSite = null;
                // If the user defined a site, load it
                if (!string.IsNullOrEmpty(siteName))
                {
                    environmentSite = await customerPortalClient.GetObjectByName<ProductSite>(siteName);
                }

                environment = new CustomerEnvironment
                {
                    Name = name,
                    Description = description,
                    Parameters = rawParameters,
                    EnvironmentType = environmentType.ToString(),
                    DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target.Value),
                    Site = environmentSite,
                };

                // check environment connection
                await CheckConnectionNewEnvironmentCreation(environment, customerInfrastructureName);

                Session.LogInformation($"Creating the customer environment {name} for a customer infrastructure...");
                environment = await customerEnvironmentServices.CreateCustomerEnvironmentForCustomerInfrastructure(environment, customerInfrastructureName, isInfrastructureAgent);

                if (!isInfrastructureAgent)
                {
                    await UpdateDeploymentPackage(environment, deploymentPackageName, softwareLicenseIds);
                }
            }
            // if not, just create a new environment
            else
            {
                ValidateCustomerEnvironmentArgs(target, siteName, deploymentPackageName, isInfrastructureAgent);

                Session.LogInformation($"Creating customer environment {name}...");

                environment = new CustomerEnvironment
                {
                    EnvironmentType = environmentType.ToString(),
                    Site = isInfrastructureAgent ? null : await customerPortalClient.GetObjectByName<ProductSite>(siteName),
                    Name = name,
                    DeploymentTarget = newEnvironmentUtilities.GetDeploymentTargetValue(target.Value),
                    Parameters = rawParameters
                };

                environment = await customerEnvironmentServices.CreateEnvironment(environment);

                if (!isInfrastructureAgent)
                {
                    await UpdateDeploymentPackage(environment, deploymentPackageName, softwareLicenseIds);
                }
            }

            Session.LogInformation($"Customer environment {name} {environment.DescribeVersion()} created...");

            // handle installation
            await environmentDeploymentHandler.Handle(interactive, environment, target, outputDir, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg);
        }

        /// <summary>
        /// Update the Deployment Package and licenses of the environment
        /// </summary>
        /// <param name="environment">The environment version</param>
        /// <param name="deploymentPackageName">Name of the Deployment Package</param>
        /// <param name="softwareLicenseIds">IDs of the supplied or preserved licenses</param>
        private async Task UpdateDeploymentPackage(CustomerEnvironment environment, string deploymentPackageName, long[] softwareLicenseIds)
        {
            DeploymentPackage deploymentPackage = await customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName);
            Session.LogInformation($"Setting deployment package {deploymentPackageName} (Id {deploymentPackage.Id}) on customer environment {environment.Name} {environment.DescribeVersion()}...");

            await customerEnvironmentServices.UpdateDeploymentPackage(
                environment,
                deploymentPackage,
                softwareLicenseIds);
        }

        /// <summary>
        /// Resolve explicit licenses, or preserve licenses from an existing environment's package relations.
        /// </summary>
        private async Task<long[]> ResolveSoftwareLicenseIds(CustomerEnvironment environment, string[] licensesNames)
        {
            if (licensesNames?.Length > 0)
            {
                return [.. (await licenseService.GetLicensesByUniqueName(licensesNames)).Select(l => l.Id)];
            }

            if (environment == null)
            {
                throw new ArgumentException("Software licenses must be supplied with --license when creating a new Customer Environment.", nameof(licensesNames));
            }

            var loadedEnvironment = await customerPortalClient.LoadObjectRelations(environment, ["CustomerEnvironmentDeploymentPackage"]);
            long[] licenseIds = [];
            if (loadedEnvironment.RelationCollection != null &&
                loadedEnvironment.RelationCollection.TryGetValue("CustomerEnvironmentDeploymentPackage", out var packageRelations))
            {
                licenseIds = [.. packageRelations.OfType<CustomerEnvironmentDeploymentPackage>()
                    .Where(relation => relation.SoftwareLicense != null && relation.SoftwareLicense.Id > 0)
                    .Select(relation => relation.SoftwareLicense.Id)
                    .Distinct()];
            }

            if (licenseIds.Length == 0)
            {
                throw new InvalidOperationException($"Customer environment {environment.Name} {environment.DescribeVersion()} has no software licenses to preserve. Supply --license when updating its deployment package.");
            }

            return licenseIds;
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

        /// <summary>
        /// Validate the arguments for the creation of a new environment
        /// </summary>
        /// <param name="target"></param>
        /// <param name="siteName"></param>
        /// <param name="deploymentPackageName"></param>
        /// <param name="licensesNames"></param>
        /// <param name="isInfrastructureAgent"></param>
        /// <exception cref="ArgumentNullException"></exception>
        private void ValidateCustomerEnvironmentArgs(DeploymentTarget? target, string siteName, string deploymentPackageName, bool isInfrastructureAgent)
        {
            if (!target.HasValue)
            {
                throw new ArgumentNullException(nameof(target), "Deployment Target is mandatory to create a new Customer Environment.");
            }

            if (!isInfrastructureAgent)
            {
                if (string.IsNullOrEmpty(siteName))
                {
                    throw new ArgumentNullException(nameof(siteName), "Name of the Site is mandatory to create a Customer Environment");
                }

                if (string.IsNullOrEmpty(deploymentPackageName))
                {
                    throw new ArgumentNullException(nameof(deploymentPackageName), "Deployment Package is mandatory to create a new Customer Environment for a customer infrastructure.");
                }
            }
        }
    }
}
