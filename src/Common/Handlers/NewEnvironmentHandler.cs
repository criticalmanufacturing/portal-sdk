using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
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
            string target,
            DirectoryInfo outputDir,
            string[] replaceTokens,
            bool interactive,
            string customerInfrastructureName,
            string description,
            string templateName,
            bool terminateOtherVersions
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

            Session.LogInformation($"Creating customer environment {name}...");

            // let's see if the environment already exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(name);
            }
            catch (CmfFaultException) { }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                if (description != null)
                {
                    environment.Description = description;
                }
                
                environment.DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName);
                environment.CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName);
                environment.DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target);
                environment.Parameters = rawParameters;
                environment.ChangeSet = null;

                environment = (await new CreateObjectVersionInput { Object = environment }.CreateObjectVersionAsync(true)).Object as CustomerEnvironment;

                // terminate other versions
                if (terminateOtherVersions)
                {
                    Session.LogInformation("Terminating other versions...");

                    var customerEnvironmentsToTerminate = await _newEnvironmentUtilities.GetOtherVersionToTerminate(environment);
                    await _customerPortalClient.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(customerEnvironmentsToTerminate,
                        new Foundation.BusinessObjects.OperationAttributeCollection
                        {
                            new Foundation.BusinessObjects.OperationAttribute
                            {
                                Name = "AuthBearer",
                                Value = $"Bearer {Session.AccessToken}"
                            }
                        });

                    // wait until they're terminated
                    await _environmentDeploymentHandler.WaitForEnvironmentsToBeTerminated(customerEnvironmentsToTerminate);

                    Session.LogInformation("Other versions terminated!");
                }
            }
            // if not, check if we are creating a new environment for an infrastructure
            else if (!string.IsNullOrWhiteSpace(customerInfrastructureName))
            {
                environment = new CustomerEnvironment
                {
                    Name = name,
                    Description = description,
                    Parameters = rawParameters,
                    EnvironmentType = environmentType.ToString(),
                    DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName)
                };

                environment = (await new CreateCustomerEnvironmentForCustomerInfrastructureInput
                {
                    CustomerInfrastructureName = customerInfrastructureName,
                    CustomerEnvironment = environment,
                    TemplateName = templateName
                }.CreateCustomerEnvironmentForCustomerInfrastructureAsync(true)).CustomerEnvironment;
            }
            // if not, just create a new environment
            else
            {
                environment = new CustomerEnvironment
                {
                    EnvironmentType = environmentType.ToString(),
                    Site = await _customerPortalClient.GetObjectByName<ProductSite>(siteName),
                    Name = name,
                    DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName),
                    CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target),
                    Parameters = rawParameters
                };

                environment = (await new CreateObjectVersionInput { Object = environment }.CreateObjectVersionAsync(true)).Object as CustomerEnvironment;
            }
            
            // handle installation
            await _environmentDeploymentHandler.Handle(interactive, environment, target, outputDir);
        }
    }
}