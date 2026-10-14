using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Moq;
using System.Data;

namespace Common.UnitTests.Services;

public class CustomerEnvironmentServicesTests
{
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<ISession> _sessionMock = new();

    private readonly CustomerEnvironmentServices _services;

    public CustomerEnvironmentServicesTests()
    {
        _services = new CustomerEnvironmentServices(
            _customerPortalClientMock.Object,
            _sessionMock.Object,
            Mock.Of<IEnvironmentUtilities>(),
            Mock.Of<IEnvironmentDeploymentHandler>());
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
