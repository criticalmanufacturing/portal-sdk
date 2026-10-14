using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("Common.UnitTests")]
namespace Cmf.CustomerPortal.Sdk.Common.Services;

internal class CustomerEnvironmentServices(
    ICustomerPortalClient customerPortalClient,
    ISession session,
    IEnvironmentUtilities newEnvironmentUtilities,
    IEnvironmentDeploymentHandler environmentDeploymentHandler) : ICustomerEnvironmentServices
{
    public async Task<CustomerEnvironment> GetCustomerEnvironment(string name)
    {
        CustomerEnvironment environment = null;
        try
        {
            environment = await customerPortalClient.GetObjectByName<CustomerEnvironment>(name);
            environment = await customerPortalClient.GetCustomerEnvironmentById(environment.Id);

            session.LogInformation($"Customer environment {name} actually exists...");
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            session.LogInformation($"Customer environment {name} doesn't exist...");
        }

        return environment;
    }

    public async Task<CustomerEnvironment> GetCustomerEnvironmentVersion(string name, int? version)
    {
        long id;
        if (version is null)
        {
            try
            {
                id = (await customerPortalClient.GetObjectByName<CustomerEnvironment>(name)).Id;
            }
            catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
            {
                throw new NotFoundException(string.Format(Resources.EnvironmentNotFoundMessage, name));
            }
        }
        else
        {
            id = await GetCustomerEnvironmentVersionId(name, version.Value)
                ?? throw new NotFoundException(string.Format(Resources.EnvironmentVersionNotFoundMessage, name, version));
        }

        session.LogDebug($"Loading customer environment {name} (Id {id})");
        return await customerPortalClient.GetCustomerEnvironmentById(id);
    }

    /// <summary>
    /// Queries the Id of the given version of a customer environment.
    /// </summary>
    /// <returns>The Id of the version, or <c>null</c> when there is no such version.</returns>
    private async Task<long?> GetCustomerEnvironmentVersionId(string name, int version)
    {
        QueryObject query = new QueryObject
        {
            EntityTypeName = "CustomerEnvironment",
            Name = "GetCustomerEnvironmentVersion",
            Query = new Query()
        };
        query.Query.Distinct = false;
        query.Query.Filters = new FilterCollection() {
            new Filter()
            {
                Name = "Name",
                ObjectName = "CustomerEnvironment",
                ObjectAlias = "CustomerEnvironment_1",
                Operator = Foundation.Common.FieldOperator.IsEqualTo,
                Value = name,
                LogicalOperator = Foundation.Common.LogicalOperator.AND,
                FilterType = Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
            },
            new Filter()
            {
                Name = "Version",
                ObjectName = "CustomerEnvironment",
                ObjectAlias = "CustomerEnvironment_1",
                Operator = Foundation.Common.FieldOperator.IsEqualTo,
                Value = version,
                LogicalOperator = Foundation.Common.LogicalOperator.Nothing,
                FilterType = Foundation.BusinessObjects.QueryObject.Enums.FilterType.Normal,
            }
        };
        query.Query.Fields = new FieldCollection() {
            new Field()
            {
                Alias = "Id",
                ObjectName = "CustomerEnvironment",
                ObjectAlias = "CustomerEnvironment_1",
                IsUserAttribute = false,
                Name = "Id",
                Position = 0,
                Sort = Foundation.Common.FieldSort.NoSort
            }
        };
        query.Query.Relations = new RelationCollection();

        DataSet dataSet = await customerPortalClient.ExecuteQuery(query);

        return dataSet?.Tables?.Count > 0 && dataSet.Tables[0].Rows.Count > 0
            ? (long)dataSet.Tables[0].Rows[0]["Id"]
            : null;
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
    public async Task TerminateOtherVersions(CustomerEnvironment customerEnvironment, bool terminateOtherVersionsRemove, bool terminateOtherVersionsRemoveVolumes, bool undeploy)
    {
        session.LogInformation("Terminating other versions...");

        var customerEnvironmentsToTerminate = await newEnvironmentUtilities.GetOtherVersionToTerminate(customerEnvironment);
        OperationAttributeCollection terminateOperationAttributes = [];

        // create a light ce et, because GetEntityTypeByName returns a huge object
        EntityType ceET = await customerPortalClient.GetEntityTypeByName("CustomerEnvironment");
        ceET = new EntityType
        {
            Id = ceET.Id,
            Name = ceET.Name
        };

        foreach (var ce in customerEnvironmentsToTerminate)
        {
            OperationAttribute attributeRemove = new()
            {
                EntityId = ce.Id,
                EntityType = ceET,
                Name = "RemoveDeployments",
                OperationName = "TerminateVersion",
                Value = terminateOtherVersionsRemove ? 1 : 0
            };

            OperationAttribute attributeRemoveVolumes = new()
            {
                EntityId = ce.Id,
                EntityType = ceET,
                Name = "RemoveVolumes",
                OperationName = "TerminateVersion",
                Value = (terminateOtherVersionsRemove && terminateOtherVersionsRemoveVolumes) ? 1 : 0
            };

            OperationAttribute attributeUndeploy = new()
            {
                EntityId = ce.Id,
                EntityType = ceET,
                Name = "Undeploy",
                OperationName = "TerminateVersion",
                Value = undeploy ? 1 : 0
            };

            terminateOperationAttributes.Add(attributeRemove);
            terminateOperationAttributes.Add(attributeRemoveVolumes);
            terminateOperationAttributes.Add(attributeUndeploy);
        }

        if (customerEnvironmentsToTerminate.Count > 0)
        {
            await customerPortalClient.TerminateCustomerEnvironments([.. customerEnvironmentsToTerminate.Select(ce => ce.Id)], terminateOperationAttributes);

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