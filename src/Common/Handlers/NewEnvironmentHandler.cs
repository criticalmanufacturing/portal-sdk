using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

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
            string licenseName,
            string deploymentPackageName,
            DeploymentTarget target,
            DirectoryInfo outputDir,
            string[] replaceTokens,
            bool interactive,
            string customerInfrastructureName,
            string description,
            bool terminateOtherVersions,
            bool isInfrastructureAgent,
            double? minutesTimeoutMainTask
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
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(name);

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
                environment.CustomerLicense = isInfrastructureAgent || string.IsNullOrWhiteSpace(licenseName) ? environment.CustomerLicense : await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName);
                environment.DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target);
                environment.Parameters = rawParameters;
                environment.ChangeSet = null;

                Session.LogInformation($"Creating a new version of the Customer environment {name}...");

                environment = await CreateEnvironment(_customerPortalClient, environment);

                // terminate other versions
                if (terminateOtherVersions)
                {
                    Session.LogInformation("Terminating other versions...");

                    var customerEnvironmentsToTerminate = await _newEnvironmentUtilities.GetOtherVersionToTerminate(environment);
                    await _customerPortalClient.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(customerEnvironmentsToTerminate);

                    // wait until they're terminated
                    await _environmentDeploymentHandler.WaitForEnvironmentsToBeTerminated(customerEnvironmentsToTerminate);

                    Session.LogInformation("Other versions terminated!");
                }
            }
            // if not, check if we are creating a new environment for an infrastructure
            else if (!string.IsNullOrWhiteSpace(customerInfrastructureName))
            {
                Session.LogInformation($"Creating the customer environment {name} for a customer infrastructure...");

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
                        throw new ArgumentNullException("Name of the Site is mandatory to create a Customer Environment");
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
                    CustomerLicense = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName)
                };

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
                    CustomerLicense = isInfrastructureAgent ? null : await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Parameters = rawParameters
                };

                environment = await CreateEnvironment(_customerPortalClient, environment);
            }

            Session.LogInformation($"Customer environment {name} created...");

            // handle installation
            await _environmentDeploymentHandler.Handle(interactive, environment, target, outputDir, minutesTimeoutMainTask);
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
                return CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Version);
            }
            catch (CmfFaultException exception) when (exception.Code?.Name == "Db20001")
            {
                // was not found on GetObjectByName, so let's create a new Entity
                return CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Entity);
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
        public static CustomerEnvironment CreateNewEnvironmentEntityOrVersion(CustomerEnvironment customerEnvironment, EntityTypeSource entityType)
        {
            customerEnvironment.ChangeSet = null;
            customerEnvironment = new CreateObjectVersionInput
            {
                Object = customerEnvironment,
                OperationTarget = entityType
            }.CreateObjectVersionSync().Object as CustomerEnvironment;
            return customerEnvironment;
        }
    }
}
