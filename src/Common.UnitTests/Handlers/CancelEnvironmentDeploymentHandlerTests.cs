using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;

namespace Common.UnitTests.Handlers;

public class CancelEnvironmentDeploymentHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerEnvironmentServices> _customerEnvironmentServicesMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();

    private readonly CancelEnvironmentDeploymentHandler _handler;

    public CancelEnvironmentDeploymentHandlerTests()
    {
        _handler = new CancelEnvironmentDeploymentHandler(
            _sessionMock.Object,
            _customerEnvironmentServicesMock.Object,
            _customerPortalClientMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentNullException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(name!));

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment(It.IsAny<string>()), Times.Never);
        _customerPortalClientMock.Verify(c => c.CancelEnvironmentDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenEnvironmentDoesNotExist_ThrowsException()
    {
        // Arrange
        _ = _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync((CustomerEnvironment?)null!);

        // Act
        Exception exception = await Assert.ThrowsAsync<Exception>(() => _handler.Run("env"));

        // Assert
        Assert.Equal("Customer environment with name 'env' does not exist...", exception.Message);
        _customerPortalClientMock.Verify(c => c.CancelEnvironmentDeployment(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenEnvironmentExists_CancelsItsDeployment()
    {
        // Arrange
        var env = new CustomerEnvironment { Id = 42, Name = "env", Version = 3 };
        _ = _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _ = _sessionMock.Setup(s => s.Configuration).Returns(PortalConfiguration.Create());

        // Act
        await _handler.Run("env");

        // Assert
        _customerPortalClientMock.Verify(c => c.CancelEnvironmentDeployment(42), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("Cancelling the deployment of customer environment env version 3 (Id 42)..."), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("https://portal.example.com/Entity/CustomerEnvironment/42/View/Installation"), Times.Once);
    }
}
