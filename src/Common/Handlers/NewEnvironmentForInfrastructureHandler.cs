using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.Foundation.Common.Licenses.Enums;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers
{
    public class NewEnvironmentForInfrastructureHandler : AbstractHandler
    {
        private readonly ICustomerPortalClient _customerPortalClient;
        private readonly INewEnvironmentUtilities _newEnvironmentUtilities;
        private readonly IEnvironmentDeploymentHandler _environmentDeploymentHandler;

        public NewEnvironmentForInfrastructureHandler(ICustomerPortalClient customerPortalClient, ISession session,
            INewEnvironmentUtilities newEnvironmentUtilities, IEnvironmentDeploymentHandler environmentDeploymentHandler) : base(session, true)
        {
            _customerPortalClient = customerPortalClient;
            _newEnvironmentUtilities = newEnvironmentUtilities;
            _environmentDeploymentHandler = environmentDeploymentHandler;
        }

        public async Task Run(
            string customerInfrastructureName,
            string customerEnvironmentName,
            string customerEnvironmentDescription,
            FileInfo parameters,
            EnvironmentType customerEnvironmentType,
            string customerEnvironmentLicenseName,
            string customerEnvironmentDeploymentPackageName,
            string customerEnvironmentDeploymentTarget,
            DirectoryInfo outputDir,
            string[] replaceTokens,
            bool interactive
        )
        {
            await EnsureLogin();

            #region Validations

            // check if customer infrastructure exists
            try
            {
                await _customerPortalClient.GetObjectByName<CustomerInfrastructure>(customerInfrastructureName);
            }
            catch (CmfFaultException)
            {
                Session.LogInformation($"Could not find a CustomerInfrastructure with name: {customerEnvironmentName}...");
                throw;
            }

            #endregion

            // generate name if needed
            customerEnvironmentName = string.IsNullOrWhiteSpace(customerEnvironmentName) ? $"Deployment-{Guid.NewGuid()}" : customerEnvironmentName;
            
            // replace tokens in environment parameters
            string rawParameters = null;
            if (parameters != null)
            {
                rawParameters = File.ReadAllText(parameters.FullName);
                rawParameters = await Utils.ReplaceTokens(Session, rawParameters, replaceTokens, true);
            }

            Session.LogInformation($"Creating customer environment {customerEnvironmentName}...");

            // create environment for customer infrastructure
            CustomerEnvironment customerEnvironment;
            try
            {
                customerEnvironment = new CustomerEnvironment
                {
                    Name = customerEnvironmentName,
                    Description = customerEnvironmentDescription,
                    Parameters = rawParameters,
                    EnvironmentType = customerEnvironmentType.ToString(),
                    CustomerLicense = await _customerPortalClient.GetObjectByName<CustomerLicense>(customerEnvironmentLicenseName),
                    DeploymentPackage = await _customerPortalClient.GetObjectByName<DeploymentPackage>(customerEnvironmentDeploymentPackageName),
                    DeploymentTarget = _newEnvironmentUtilities.GetDeploymentTargetValue(customerEnvironmentDeploymentTarget)
                };

                customerEnvironment = (await new CreateCustomerEnvironmentForCustomerInfrastructureInput
                {
                    CustomerInfrastructureName = customerInfrastructureName,
                    CustomerEnvironment = customerEnvironment
                }.CreateCustomerEnvironmentForCustomerInfrastructureAsync(true)).CustomerEnvironment;
            }
            catch (Exception ex)
            {
                Session.LogError($"Error while creating customer environment: {ex.Message}");
                throw;
            }

            // deployment
            await _environmentDeploymentHandler.Handle(interactive, customerEnvironment, customerEnvironmentDeploymentTarget, outputDir);
        }
    }
}
