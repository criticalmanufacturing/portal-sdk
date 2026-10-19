using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Moq;
using System.Data;

namespace Common.UnitTests.Services;

public class CustomerEnvironmentServicesTests
{
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<IEnvironmentUtilities> _environmentUtilitiesMock = new();
    private readonly Mock<IEnvironmentDeploymentHandler> _environmentDeploymentHandlerMock = new();

    private readonly CustomerEnvironmentServices _services;

    public CustomerEnvironmentServicesTests()
    {
        _services = new CustomerEnvironmentServices(
            _customerPortalClientMock.Object,
            _sessionMock.Object,
            _environmentUtilitiesMock.Object,
            _environmentDeploymentHandlerMock.Object);
    }

    [Fact]
    public async Task GetCustomerEnvironment_WhenEnvironmentExists_LogsItsCurrentVersion()
    {
        // Arrange
        var environment = new CustomerEnvironment { Id = 10, Name = "env", Version = 3 };
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ReturnsAsync(new CustomerEnvironment { Id = 10, Name = "env" });
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentById(10, 0))
            .ReturnsAsync(environment);

        // Act
        CustomerEnvironment result = await _services.GetCustomerEnvironment("env");

        // Assert
        Assert.Same(environment, result);
        _sessionMock.Verify(s => s.LogInformation("Customer environment env exists, current version 3 (Id 10)..."), Times.Once);
    }

    [Fact]
    public async Task TerminateOtherVersions_WhenThereAreVersionsToTerminate_LogsTheirVersions()
    {
        // Arrange
        var currentVersion = new CustomerEnvironment { Id = 30, Name = "env", Version = 3 };
        SetupVersionsToTerminate(currentVersion,
            new CustomerEnvironment { Id = 20, Name = "env", Version = 2 },
            new CustomerEnvironment { Id = 10, Name = "env", Version = 1 });
        _environmentDeploymentHandlerMock.Setup(h => h.WaitForEnvironmentsToBeTerminated(It.IsAny<CustomerEnvironmentCollection>()))
            .ReturnsAsync([]);

        // Act
        await _services.TerminateOtherVersions(currentVersion, false, false, false);

        // Assert
        _customerPortalClientMock.Verify(c => c.TerminateCustomerEnvironments(It.Is<List<long>>(ids => ids.SequenceEqual(new long[] { 20, 10 })), It.IsAny<OperationAttributeCollection>()), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("Terminating version 2 (Id 20), version 1 (Id 10) of customer environment env; keeping version 3 (Id 30)..."), Times.Once);
    }

    [Fact]
    public async Task TerminateOtherVersions_WhenTerminationFails_LogsTheTerminationLogsOfTheFailedVersionsAndThrows()
    {
        // Arrange
        var currentVersion = new CustomerEnvironment { Id = 30, Name = "env", Version = 3 };
        SetupVersionsToTerminate(currentVersion,
            new CustomerEnvironment { Id = 10, Name = "env", Version = 1 },
            new CustomerEnvironment { Id = 20, Name = "env", Version = 2 });
        _environmentDeploymentHandlerMock.Setup(h => h.WaitForEnvironmentsToBeTerminated(It.IsAny<CustomerEnvironmentCollection>()))
            .ReturnsAsync([20]);
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentTerminationLogs(20))
            .ReturnsAsync("termination logs");

        // Act
        var exception = await Assert.ThrowsAsync<Exception>(() => _services.TerminateOtherVersions(currentVersion, false, false, false));

        // Assert
        Assert.Equal("Stopping deploy process because termination of other environment versions failed. Environment Ids of failed terminations: 20.", exception.Message);
        _sessionMock.Verify(s => s.LogError("\nCustomer Environment env version 2 (Id 20) did not terminate successfully. Termination logs:\n termination logs\n"), Times.Once);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentTerminationLogs(10), Times.Never);
    }

    [Fact]
    public async Task GetCustomerEnvironmentVersion_WithoutVersion_LoadsTheCurrentVersion()
    {
        // Arrange
        var environment = new CustomerEnvironment { Id = 10, Name = "env" };
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ReturnsAsync(new CustomerEnvironment { Id = 10, Name = "env" });
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentById(10, 0))
            .ReturnsAsync(environment);

        // Act
        CustomerEnvironment result = await _services.GetCustomerEnvironmentVersion("env", null);

        // Assert
        Assert.Same(environment, result);
        _customerPortalClientMock.Verify(c => c.ExecuteQuery(It.IsAny<QueryObject>()), Times.Never);
        _sessionMock.Verify(s => s.LogInformation(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task GetCustomerEnvironmentVersion_WithoutVersion_WhenEnvironmentDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ThrowsAsync(new CmfFaultException("action", new CmfFaultCode("Db20001", null), null, "not found"));

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _services.GetCustomerEnvironmentVersion("env", null));

        // Assert
        Assert.Equal("Customer Environment 'env' does not exist", exception.Message);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentById(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCustomerEnvironmentVersion_WithVersion_QueriesTheVersionByNameAndLoadsIt()
    {
        // Arrange
        var environment = new CustomerEnvironment { Id = 7, Name = "env", Version = 3 };
        QueryObject? query = null;
        _customerPortalClientMock.Setup(c => c.ExecuteQuery(It.IsAny<QueryObject>()))
            .Callback<QueryObject>(q => query = q)
            .ReturnsAsync(IdsDataSet(7));
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentById(7, 0))
            .ReturnsAsync(environment);

        // Act
        CustomerEnvironment result = await _services.GetCustomerEnvironmentVersion("env", 3);

        // Assert
        Assert.Same(environment, result);
        Assert.NotNull(query);
        Assert.Equal("CustomerEnvironment", query.EntityTypeName);
        Assert.Equal("env", Assert.Single(query.Query.Filters, f => f.Name == "Name").Value);
        Assert.Equal(3, Assert.Single(query.Query.Filters, f => f.Name == "Version").Value);
        _customerPortalClientMock.Verify(c => c.GetObjectByName<CustomerEnvironment>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        _sessionMock.Verify(s => s.LogInformation(It.IsAny<string>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public async Task GetCustomerEnvironmentVersion_WithLevelsToLoad_LoadsTheVersionWithItsRelatedObjects(int? version)
    {
        // Arrange
        var environment = new CustomerEnvironment { Id = 7, Name = "env", Version = 3, Site = new ProductSite { Name = "site" } };
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ReturnsAsync(new CustomerEnvironment { Id = 7, Name = "env" });
        _customerPortalClientMock.Setup(c => c.ExecuteQuery(It.IsAny<QueryObject>()))
            .ReturnsAsync(IdsDataSet(7));
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentById(7, 1))
            .ReturnsAsync(environment);

        // Act
        CustomerEnvironment result = await _services.GetCustomerEnvironmentVersion("env", version, levelsToLoad: 1);

        // Assert
        Assert.Same(environment, result);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentById(7, 1), Times.Once);
    }

    [Fact]
    public async Task GetCustomerEnvironmentVersion_WithVersion_WhenVersionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _customerPortalClientMock.Setup(c => c.ExecuteQuery(It.IsAny<QueryObject>()))
            .ReturnsAsync(IdsDataSet());

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _services.GetCustomerEnvironmentVersion("env", 3));

        // Assert
        Assert.Equal("Customer Environment 'env' has no version 3", exception.Message);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentById(It.IsAny<long>(), It.IsAny<int>()), Times.Never);
    }

    private void SetupVersionsToTerminate(CustomerEnvironment currentVersion, params CustomerEnvironment[] versionsToTerminate)
    {
        _environmentUtilitiesMock.Setup(u => u.GetOtherVersionToTerminate(currentVersion))
            .ReturnsAsync([.. versionsToTerminate]);
        _customerPortalClientMock.Setup(c => c.GetEntityTypeByName("CustomerEnvironment"))
            .ReturnsAsync(new EntityType { Id = 1, Name = "CustomerEnvironment" });
    }

    private static DataSet IdsDataSet(params long[] ids)
    {
        var table = new DataTable();
        table.Columns.Add("Id", typeof(long));
        foreach (long id in ids)
        {
            table.Rows.Add(id);
        }

        var dataSet = new DataSet();
        dataSet.Tables.Add(table);
        return dataSet;
    }
}
