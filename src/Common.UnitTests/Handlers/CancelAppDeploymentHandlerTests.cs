using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Moq;

namespace Common.UnitTests.Handlers;

public class CancelAppDeploymentHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<IEnvironmentUtilities> _environmentUtilitiesMock = new();

    private readonly CancelAppDeploymentHandler _handler;

    public CancelAppDeploymentHandlerTests()
    {
        _handler = new CancelAppDeploymentHandler(
            _sessionMock.Object,
            _customerPortalClientMock.Object,
            _environmentUtilitiesMock.Object);
    }

    [Theory]
    [InlineData(null, "env")]
    [InlineData(" ", "env")]
    [InlineData("my-app", null)]
    [InlineData("my-app", " ")]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentNullException(string? appName, string? customerEnvironmentName)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(appName!, customerEnvironmentName!));

        _customerPortalClientMock.Verify(c => c.CancelAppDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenEnvironmentDoesNotExist_ThrowsException()
    {
        // Arrange
        _ = _environmentUtilitiesMock
            .Setup(u => u.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync((CustomerEnvironment?)null);

        // Act
        Exception exception = await Assert.ThrowsAsync<Exception>(() => _handler.Run("my-app", "env"));

        // Assert
        Assert.Equal("Customer environment 'env' was not found or has no deployed versions.", exception.Message);
        _customerPortalClientMock.Verify(c => c.CancelAppDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenAppIsNotInstalled_ThrowsException()
    {
        // Arrange
        CustomerEnvironment env = EnvironmentWithApp("other-app", 123);

        // Act
        Exception exception = await Assert.ThrowsAsync<Exception>(() => _handler.Run("my-app", "env"));

        // Assert
        Assert.Equal("App 'my-app' is not installed in customer environment 'env'.", exception.Message);
        _customerPortalClientMock.Verify(c => c.CancelAppDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenEnvironmentHasNoApps_ThrowsException()
    {
        // Arrange
        var env = new CustomerEnvironment { Status = DeploymentStatus.DeploymentSucceeded };
        SetupEnvironment(env);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _handler.Run("my-app", "env"));

        _customerPortalClientMock.Verify(c => c.CancelAppDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenAppIsInstalled_CancelsItsDeployment()
    {
        // Arrange
        _ = EnvironmentWithApp("my-app", 123);

        // Act
        await _handler.Run("my-app", "env");

        // Assert
        _customerPortalClientMock.Verify(c => c.CancelAppDeployment(123), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("Cancelling the deployment of app 'my-app' (installation Id 123) in customer environment 'env' version 2 (Id 5)..."), Times.Once);
    }

    private CustomerEnvironment EnvironmentWithApp(string appName, long relationId)
    {
        var ceap = new CustomerEnvironmentApplicationPackage
        {
            Id = relationId,
            TargetEntity = new ApplicationPackage { Name = appName }
        };

        var env = new CustomerEnvironment
        {
            Id = 5,
            Version = 2,
            Status = DeploymentStatus.DeploymentSucceeded,
            RelationCollection = new CmfEntityRelationCollection
            {
                ["CustomerEnvironmentApplicationPackage"] = new EntityRelationCollection { ceap }
            }
        };

        SetupEnvironment(env);
        return env;
    }

    private void SetupEnvironment(CustomerEnvironment env)
    {
        _ = _environmentUtilitiesMock
            .Setup(u => u.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);

        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);
    }
}
