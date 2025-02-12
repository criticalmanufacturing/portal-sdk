using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Common.Deployment;
using Cmf.CustomerPortal.Orchestration.CustomerEnvironmentManagement.InputObjects;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessOrchestration.GenericServiceManagement.InputObjects;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Services;

internal class CustomerEnvironmentServices : ICustomerEnvironmentServices
{
    public async Task<CustomerEnvironment> GetCustomerEnvironment(ISession session, string name)
    {
        CustomerEnvironment environment = null;
        try
        {
            environment = (await(new GetCustomerEnvironmentByNameInput() { CustomerEnvironmentName = name }.GetCustomerEnvironmentByNameAsync(true))).CustomerEnvironment;

            session.LogInformation($"Customer environment {name} actually exists...");
        }
        catch (CmfFaultException ex) when (ex.Code?.Name == Foundation.Common.CmfExceptionType.Db20001.ToString())
        {
            session.LogInformation($"Customer environment {name} doesn't exist...");
        }

        return environment;
    }

    public async Task<CustomerEnvironment> CreateEnvironment(ICustomerPortalClient client, CustomerEnvironment customerEnvironment)
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
        customerEnvironment.ChangeSet = null;
        return (await new UpdateCustomerEnvironmentInput
        {
            CustomerEnvironment = customerEnvironment,
            DeploymentParametersMergeMode = DeploymentParametersMergeMode.Merge
        }.UpdateCustomerEnvironmentAsync(true)).CustomerEnvironment;
    }

    public async Task<CustomerEnvironment> CreateCustomerEnvironmentForCustomerInfrastructure(CustomerEnvironment environment, string customerInfrastructureName, bool isInfrastructureAgent)
        => (await new CreateCustomerEnvironmentForCustomerInfrastructureInput
        {
            CustomerInfrastructureName = customerInfrastructureName,
            CustomerEnvironment = environment,
            IsInfrastructureAgent = isInfrastructureAgent
        }.CreateCustomerEnvironmentForCustomerInfrastructureAsync(true)).CustomerEnvironment;

    public async Task UpdateDeploymentPackage(CustomerEnvironment customerEnvironment, DeploymentPackage deploymentPackage, long[] softwareLicensesIds)
        => await new UpdateCustomerEnvironmentDeploymentPackageInput
        {
            CustomerEnvironmentId = customerEnvironment.Id,
            DeploymentPackageId = deploymentPackage.Id,
            SoftwareLicenseIds = softwareLicensesIds
        }.UpdateCustomerEnvironmentDeploymentPackageAsync(true);
}
