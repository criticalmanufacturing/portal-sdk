using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System.Linq;
using Cmf.CustomerPortal.Common.Deployment;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironmentHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;
        private readonly INewEnvironmentUtilities _newEnvironmentUtilities;
        private readonly IEnvironmentDeploymentHandler _environmentDeploymentHandler;

        public NewEnvironmentHandler(ICustomerPortalClient customerPortalClient, ISession session,
            INewEnvironmentUtilities newEnvironmentUtilities, IEnvironmentDeploymentHandler environmentDeploymentHandler) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
            _newEnvironmentUtilities = newEnvironmentUtilities;
            _environmentDeploymentHandler = environmentDeploymentHandler;
        }

        public async Task Run(
            string name,
            FileInfo parameters,
            EnvironmentType environmentType,
            string siteName,
            long licenseId,
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
                rawParameters = File.ReadAllText(parameters.FullName);
                rawParameters = await Utils.ReplaceTokens(Session, rawParameters, replaceTokens, true);
            }

            Session.LogInformation($"Checking if customer environment {name} exists...");
            // let's see if the environment already exists
            CustomerEnvironment environment = null;
            try
            {
                environment = (await (new GetCustomerEnvironmentByNameInput() { CustomerEnvironmentName = name }.GetCustomerEnvironmentByNameAsync(true))).CustomerEnvironment;

                Session.LogInformation($"Customer environment {name} actually exists...");
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
            {
                Session.LogInformation($"Customer environment {name} doesn't exist...");
            }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                if (description != null)
                {
                    environment.Description = description;
                }

                environment.DeploymentPackage = isInfrastructureAgent || string.IsNullOrWhiteSpace(deploymentPackageName) ? environment.DeploymentPackage : await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName);
                environment.CustomerLicense = isInfrastructureAgent || licenseId > 0 ? environment.CustomerLicense : await _customerPortalClient.GetObjectById<CustomerLicense>(licenseId);
                environment.DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target);
                environment.ChangeSet = null;

                // check environment connection
                await _newEnvironmentUtilities.CheckEnvironmentConnection(environment);

                Session.LogInformation($"Creating a new version of the Customer environment {name}...");
                environment = await CreateEnvironment(_customerPortalClient, environment);

                // Update environment with the parameters to be merged instead of overwriting
                environment.Parameters = rawParameters;
                environment = await UpdateEnvironment(environment);

                // terminate other versions
                if (terminateOtherVersions)
                {
                    Session.LogInformation("Terminating other versions...");

                    var customerEnvironmentsToTerminate = await _newEnvironmentUtilities.GetOtherVersionToTerminate(environment);
                    OperationAttributeCollection terminateOperationAttibutes = new OperationAttributeCollection();
                    EntityType ceET = (await new GetEntityTypeByNameInput { Name = "CustomerEnvironment" }.GetEntityTypeByNameAsync(true)).EntityType;
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
                        await _customerPortalClient.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(customerEnvironmentsToTerminate, terminateOperationAttibutes);

                        // wait until they're terminated
                        List<long> ceTerminationFailedIds = await _environmentDeploymentHandler.WaitForEnvironmentsToBeTerminated(customerEnvironmentsToTerminate);

                        if (ceTerminationFailedIds?.Count > 0)
                        {
                            string failedIdsString = string.Join(", ", ceTerminationFailedIds.Select(x => x.ToString()));
                            string errorMessage = $"Stopping deploy process because termination of other environment versions failed. Environment Ids of failed terminations: {failedIdsString}.";
                            Exception ex = new Exception(errorMessage);
                            Session.LogError(ex);

                            foreach (long ceId in ceTerminationFailedIds)
                            {
                                var output = await (new GetCustomerEnvironmentByIdInput() { CustomerEnvironmentId = ceId }.GetCustomerEnvironmentByIdAsync(true));
                                CustomerEnvironment ce = output.CustomerEnvironment;
                                Session.LogError($"\nCustomer Environment {ce.Id} did not terminate sucessully. Termination logs:\n {ce.TerminationLogs}\n");
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
                        environmentSite = await _customerPortalClient.GetObjectByName<ProductSite>(siteName);
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
                    DeploymentPackage = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Site = environmentSite,
                    CustomerLicense = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectById<CustomerLicense>(licenseId)
                };

                // check environment connection
                await CheckConnectionNewEnvironmentCreation(environment, customerInfrastructureName);

                Session.LogInformation($"Creating the customer environment {name} for a customer infrastructure...");

                environment = (await new CreateCustomerEnvironmentForCustomerInfrastructureInput
                {
                    CustomerInfrastructureName = customerInfrastructureName,
                    CustomerEnvironment = environment,
                    IsInfrastructureAgent = isInfrastructureAgent
                }.CreateCustomerEnvironmentForCustomerInfrastructureAsync(true)).CustomerEnvironment;
            }
            // if not, just create a new environment
            else
            {
                Session.LogInformation($"Creating customer environment {name}...");

                environment = new CustomerEnvironment
                {
                    EnvironmentType = environmentType.ToString(),
                    Site = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectByName<ProductSite>(siteName),
                    Name = name,
                    DeploymentPackage = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                    CustomerLicense = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectById<CustomerLicense>(licenseId),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Parameters = rawParameters
                };

                environment = await CreateEnvironment(_customerPortalClient, environment);
            }

            Session.LogInformation($"Customer environment {name} created...");


            // handle installation
            await _environmentDeploymentHandler.Handle(interactive, environment, target, outputDir, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg);
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
            await _newEnvironmentUtilities.CheckEnvironmentConnection(newEnvironment);
        }

        /// <summary>
        /// Creates the environment or a new version if it already exists.
        /// </summary>
        /// <param name="customerEnvironment">The customer environment.</param>
        /// <returns>The created environment.</returns>
        public static async Task<CustomerEnvironment> CreateEnvironment(ICustomerPortalClient client, CustomerEnvironment customerEnvironment)
        {
            try
            {
                CustomerEnvironment ceFound = await client.GetObjectByName<CustomerEnvironment>(customerEnvironment.Name);

                // was found on GetObjectByName, so let's create a new version
                return await CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Version);
            }
            catch (CmfFaultException exception) when (exception.Code?.Name == "Db20001")
            {
                // was not found on GetObjectByName, so let's create a new Entity
                return await CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Entity);
            }
        }

        /// <summary>
        /// Creates new customer environment entity or a new customer environment version.
        /// A new entity (entityType = Entity) of a customer environment makes sense when the customer environment doesn't exists and we want to create a new customer environment entity with the first version.
        /// To create a new customer environment version (entityType = Version) makes sense when the entity (with the first version) already exists.
        /// </summary>
        /// <param name="customerEnvironment">customer environment to create new entity</param>
        /// <param name="entityType">entityType for operation target (Entity -> for first version and entity creation | Version -> for a new version)</param>
        /// <returns>new customer environment with the first version</returns>
        public static async Task<CustomerEnvironment> CreateNewEnvironmentEntityOrVersion(CustomerEnvironment customerEnvironment, EntityTypeSource entityType)
        {
            customerEnvironment.ChangeSet = null;
            customerEnvironment = (await new CreateObjectVersionInput
            {
                Object = customerEnvironment,
                OperationTarget = entityType
            }.CreateObjectVersionAsync(true)).Object as CustomerEnvironment;
            return customerEnvironment;
        }

        /// <summary>
        /// Update a customer environment.
        /// </summary>
        /// <param name="customerEnvironment">customer environment</param>
        /// <returns></returns>
        public static async Task<CustomerEnvironment> UpdateEnvironment(CustomerEnvironment customerEnvironment)
        {
            customerEnvironment.ChangeSet = null;
            return (await new UpdateCustomerEnvironmentInput
            {
                CustomerEnvironment = customerEnvironment,
                DeploymentParametersMergeMode = DeploymentParametersMergeMode.Merge
            }.UpdateCustomerEnvironmentAsync(true)).CustomerEnvironment;
        }
    }
}
