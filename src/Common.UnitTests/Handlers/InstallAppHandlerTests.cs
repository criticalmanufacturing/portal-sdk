using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Common.UnitTests.Handlers;

public class InstallAppHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<IAppInstallationHandler> _appInstallationHandlerMock = new();

    private readonly InstallAppHandler _handler;

    public InstallAppHandlerTests()
    {
        _handler = new InstallAppHandler(
            _customerPortalClientMock.Object,
            _sessionMock.Object,
            new MockFileSystem(),
            Mock.Of<IEnvironmentUtilities>(),
            _appInstallationHandlerMock.Object);
    }

    [Fact]
    public async Task Run_InstallsTheAppAndLogsItsInstallationAndTheEnvironmentVersion()
    {
        // Arrange
        var environment = new CustomerEnvironment { Name = "env", Id = 7, Version = 3, DeploymentTarget = "KubernetesOnPremisesTarget" };
        var customerEnvironmentApplicationPackage = new CustomerEnvironmentApplicationPackage { Id = 123 };
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ReturnsAsync(environment);
        _customerPortalClientMock.Setup(c => c.CreateOrUpdateAppInstallation(7, "my-app", "1.0.0", null!, "license"))
            .ReturnsAsync(customerEnvironmentApplicationPackage);

        // Act
        await _handler.Run("my-app", "1.0.0", "env", "license", null!, [], null!, null);

        // Assert
        _sessionMock.Verify(
            s => s.LogInformation(
                "Installing app 'my-app' version 1.0.0 (Id 123) in customer environment 'env' version 3 (Id 7)..."
            ),
            Times.Once
        );
        _appInstallationHandlerMock.Verify(h => h.Handle("my-app", customerEnvironmentApplicationPackage, "KubernetesOnPremisesTarget", null!, null, null), Times.Once);
    }
}
