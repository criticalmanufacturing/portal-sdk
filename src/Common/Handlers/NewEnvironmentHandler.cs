using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
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
            bool interactive
        )
        {
            await EnsureLogin();

            name = string.IsNullOrWhiteSpace(name) ? $"Deployment-{Guid.NewGuid()}" : name;
            string rawParameters = null;

            if (parameters != null)
            {
                rawParameters = File.ReadAllText(parameters.FullName);
                rawParameters = await Utils.ReplaceTokens(Session, rawParameters, replaceTokens, true);
            }

            Session.LogInformation($"Creating customer environment {name}...");

            // let's see if it exists
            CustomerEnvironment environment = null;
            try
            {
                environment = await _customerPortalClient.GetObjectByName<CustomerEnvironment>(name);
            }
            catch (CmfFaultException) { }

            // if it exists, maintain everything that is definition (name, type, site), change everything else and create new version
            if (environment != null)
            {
                environment.DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(deploymentPackageName);
                environment.CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(licenseName);
                environment.DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(target);
                environment.Parameters = rawParameters;
                environment.ChangeSet = null;
            }
            // if not, just build a new complete object and create it
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
            }
            environment = (await new CreateObjectVersionInput { Object = environment }.CreateObjectVersionAsync(true)).Object as CustomerEnvironment;

            await _environmentDeploymentHandler.Handle(interactive, environment, target, outputDir);
        }
    }
}