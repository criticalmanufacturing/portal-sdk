using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Common.Deployment;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.EntityTypeManagement.InputObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Common.UnitTests")]
namespace Cmf.CustomerPortal.Sdk.Common.Services;

internal class CustomerEnvironmentServices(
    ICustomerPortalClient customerPortalClient,
    ISession session,
    INewEnvironmentUtilities newEnvironmentUtilities,
    IEnvironmentDeploymentHandler environmentDeploymentHandler) : AbstractHandler(session, true), ICustomerEnvironmentServices
{
    public async Task<CustomerEnvironment> GetCustomerEnvironment(string name)
    {
        CustomerEnvironment environment = null;
        try
        {
            environment = await customerPortalClient.GetObjectByName<CustomerEnvironment>(name);

            session.LogInformation($"Customer environment {name} actually exists...");
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            session.LogInformation($"Customer environment {name} doesn't exist...");
        }

        return environment;
    }

    public async Task<CustomerEnvironment> CreateEnvironment(CustomerEnvironment customerEnvironment)
    {
        try
        {
            CustomerEnvironment ceFound = await customerPortalClient.GetObjectByName<CustomerEnvironment>(customerEnvironment.Name);

            // was found on GetObjectByName, so let's create a new version
            return await CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Version);
        }
        catch (CmfFaultException exception) when (exception.Code?.Name == "Db20001")
        {
            // was not found on GetObjectByName, so let's create a new Entity
            return await CreateNewEnvironmentEntityOrVersion(customerEnvironment, EntityTypeSource.Entity);
        }
    }

    public async Task<CustomerEnvironment> CreateNewEnvironmentEntityOrVersion(CustomerEnvironment customerEnvironment, EntityTypeSource entityType)
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
    public async Task<CustomerEnvironment> UpdateEnvironment(CustomerEnvironment customerEnvironment)
    {
        return await customerPortalClient.UpdateEnvironment(customerEnvironment);
    }

    /// <inheritdoc/>
    public async Task<CustomerEnvironment> CreateCustomerEnvironmentForCustomerInfrastructure(CustomerEnvironment environment, string customerInfrastructureName, bool isInfrastructureAgent)
        => (await new CreateCustomerEnvironmentForCustomerInfrastructureInput
        {
            CustomerInfrastructureName = customerInfrastructureName,
            CustomerEnvironment = environment,
            IsInfrastructureAgent = isInfrastructureAgent
        }.CreateCustomerEnvironmentForCustomerInfrastructureAsync(true)).CustomerEnvironment;

    /// <inheritdoc/>
    public async Task UpdateDeploymentPackage(CustomerEnvironment customerEnvironment, DeploymentPackage deploymentPackage, long[] softwareLicensesIds)
        => await new UpdateCustomerEnvironmentDeploymentPackageInput
        {
            CustomerEnvironmentId = customerEnvironment.Id,
            DeploymentPackageId = deploymentPackage.Id,
            SoftwareLicenseIds = softwareLicensesIds
        }.UpdateCustomerEnvironmentDeploymentPackageAsync(true);

    /// <inheritdoc/>
    public async Task TerminateOtherVersions(CustomerEnvironment customerEnvironment, bool terminateOtherVersionsRemove, bool terminateOtherVersionsRemoveVolumes)
    {
        session.LogInformation("Terminating other versions...");

        var customerEnvironmentsToTerminate = await newEnvironmentUtilities.GetOtherVersionToTerminate(customerEnvironment);
        OperationAttributeCollection terminateOperationAttributes = new OperationAttributeCollection();
        EntityType ceET = await customerPortalClient.GetEntityTypeByName("CustomerEnvironment");
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

            terminateOperationAttributes.Add(attributeRemove);
            terminateOperationAttributes.Add(attributeRemoveVolumes);
        }
        if (customerEnvironmentsToTerminate.Count > 0)
        {
            await customerPortalClient.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(customerEnvironmentsToTerminate, terminateOperationAttributes);

            // wait until they're terminated
            List<long> ceTerminationFailedIds = await environmentDeploymentHandler.WaitForEnvironmentsToBeTerminated(customerEnvironmentsToTerminate);

            if (ceTerminationFailedIds?.Count > 0)
            {
                string failedIdsString = string.Join(", ", ceTerminationFailedIds.Select(x => x.ToString()));
                string errorMessage = $"Stopping deploy process because termination of other environment versions failed. Environment Ids of failed terminations: {failedIdsString}.";
                Exception ex = new(errorMessage);
                session.LogError(ex);

                foreach (long ceId in ceTerminationFailedIds)
                {
                    var logs = await customerPortalClient.GetCustomerEnvironmentTerminationLogs(ceId);
                    session.LogError($"\nCustomer Environment {ceId} did not terminate sucessully. Termination logs:\n {logs}\n");
                }

                throw ex;
            }

            session.LogInformation("Other versions terminated!");
        }
        else
        {
            session.LogInformation("There are no versions with an eligible status to be terminated.");
        }
    }
}