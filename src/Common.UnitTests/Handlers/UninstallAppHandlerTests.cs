using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
using Moq;

namespace Common.UnitTests.Handlers;

public class UninstallAppHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<IEnvironmentUtilities> _environmentUtilities = new();
    private readonly Mock<IAppUninstallationHandler> _appUninstallationHandler = new();
    private readonly UninstallAppHandler _handler;

    public UninstallAppHandlerTests()
    {
        _handler = new UninstallAppHandler(
            _sessionMock.Object,
            _customerPortalClientMock.Object,
            _environmentUtilities.Object,
            _appUninstallationHandler.Object
        );
    }

    [Fact]
    public async Task Run_WhenEnvironmentIsNotInstalled_LogsErrorAndReturns()
    {
        // Arrange
        var env = new CustomerEnvironment
        {
            Status = DeploymentStatus.NotDeployed
        };

        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);

        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);

        // Act
        await _handler.Run(
            appName: "my-app",
            customerEnvironmentName: "env",
            removeVolumes: false,
            undeploy: false,
            timeout: null,
            timeoutToGetSomeMBMessage: null
        );

        // Assert
        _sessionMock.Verify(
            s => s.LogError("Customer environment 'env' is not deployed; nothing to uninstall."),
            Times.Once
        );
        _appUninstallationHandler.Verify(
            c => c.Handle(
                It.IsAny<CustomerEnvironmentApplicationPackage>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<double?>(),
                It.IsAny<double?>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Run_WhenAppNotInstalled_LogsErrorAndReturns()
    {
        // Arrange
        var env = new CustomerEnvironment
        {
            Status = DeploymentStatus.DeploymentSucceeded
        };

        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);

        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);

        // Act
        await _handler.Run(
            appName: "missing-app",
            customerEnvironmentName: "env",
            removeVolumes: false,
            undeploy: false,
            timeout: null,
            timeoutToGetSomeMBMessage: null
        );

        // Assert
        _sessionMock.Verify(
            s => s.LogError(
                "App 'missing-app' is not installed in the latest deployed version of the customer environment 'env'."
            ),
            Times.Once
        );
        _appUninstallationHandler.Verify(
            c => c.Handle(
                It.IsAny<CustomerEnvironmentApplicationPackage>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<double?>(),
                It.IsAny<double?>()
            ),
            Times.Never
        );
    }

    [Fact]
    public async Task Run_WhenEnvironmentIsTerminated_LogsErrorAndReturns()
    {
        // Arrange
        var env = new CustomerEnvironment
        {
            UniversalState = Cmf.Foundation.Common.Base.UniversalState.Terminated
        };

        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);

        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);

        // Act
        await _handler.Run(
            appName: "any-app",
            customerEnvironmentName: "env",
            removeVolumes: false,
            undeploy: false,
            timeout: null,
            timeoutToGetSomeMBMessage: null
        );

        // Assert
        _sessionMock.Verify(
            s => s.LogError("Customer environment 'env' is terminated; uninstall cannot proceed."),
            Times.Once
        );
        _appUninstallationHandler.Verify(
            c => c.Handle(
                It.IsAny<CustomerEnvironmentApplicationPackage>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<double?>(),
                It.IsAny<double?>()
            ),
            Times.Never
        );
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Run_WhenAppExistsInEnvironment_LogsFoundAndAttemptsUninstall(bool undeploy)
    {
        // Arrange
        var ceap = new CustomerEnvironmentApplicationPackage
        {
            Id = 123,
            TargetEntity = new ApplicationPackage { Name = "my-app" }
        };

        var relations = new CmfEntityRelationCollection
        {
            ["CustomerEnvironmentApplicationPackage"] = new EntityRelationCollection { ceap }
        };

        var env = new CustomerEnvironment
        {
            Id = 5,
            Version = 2,
            Status = DeploymentStatus.DeploymentSucceeded,
            RelationCollection = relations
        };

        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);

        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);
        _ = _appUninstallationHandler
            .Setup(c => c.Handle(ceap, undeploy, undeploy, null, null))
            .Returns(Task.CompletedTask);


        // Act
        await _handler.Run(
            appName: "my-app",
            customerEnvironmentName: "env",
            removeVolumes: undeploy,
            undeploy: undeploy,
            timeout: null,
            timeoutToGetSomeMBMessage: null
        );

        // Assert
        _sessionMock.Verify(
            s => s.LogInformation("App 'my-app' (Id 123) found in customer environment 'env' version 2 (Id 5)."),
            Times.Once
        );
        _appUninstallationHandler.Verify(c => c.Handle(ceap, undeploy, undeploy, null, null), Times.Once);
    }

    [Fact]
    public async Task Run_WithoutPurgeOrFlags_OnlyRemovesWorkloads()
    {
        // Arrange
        var ceap = SetupInstalledApp();

        // Act
        await _handler.Run(
            appName: "my-app",
            customerEnvironmentName: "env",
            removeVolumes: false,
            undeploy: false,
            timeout: 10,
            timeoutToGetSomeMBMessage: 5
        );

        // Assert
        _appUninstallationHandler.Verify(c => c.Handle(ceap, false, false, 10, 5), Times.Once);
        _sessionMock.Verify(s => s.LogInformation(It.Is<string>(m => m.StartsWith("Purging App"))), Times.Never);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public async Task Run_WithPurge_RemovesVolumesAndRunsUndeployProcedures(bool removeVolumes, bool undeploy)
    {
        // Arrange
        var ceap = SetupInstalledApp();

        // Act
        await _handler.Run(
            appName: "my-app",
            customerEnvironmentName: "env",
            removeVolumes: removeVolumes,
            undeploy: undeploy,
            timeout: 10,
            timeoutToGetSomeMBMessage: 5,
            purge: true
        );

        // Assert
        _sessionMock.Verify(
            s => s.LogInformation("Purging App 'my-app' (Id 123): its volumes will be removed and its undeploy procedures executed."),
            Times.Once
        );
        _appUninstallationHandler.Verify(c => c.Handle(ceap, true, true, 10, 5), Times.Once);
    }

    [Fact]
    public async Task Run_WithPurgeWhenAppNotInstalled_DoesNotUninstall()
    {
        // Arrange
        var env = new CustomerEnvironment
        {
            Status = DeploymentStatus.DeploymentSucceeded
        };
        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);
        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);

        // Act
        await _handler.Run(
            appName: "missing-app",
            customerEnvironmentName: "env",
            removeVolumes: false,
            undeploy: false,
            timeout: null,
            timeoutToGetSomeMBMessage: null,
            purge: true
        );

        // Assert
        _appUninstallationHandler.Verify(
            c => c.Handle(
                It.IsAny<CustomerEnvironmentApplicationPackage>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<double?>(),
                It.IsAny<double?>()
            ),
            Times.Never
        );
    }

    private CustomerEnvironmentApplicationPackage SetupInstalledApp()
    {
        var ceap = new CustomerEnvironmentApplicationPackage
        {
            Id = 123,
            TargetEntity = new ApplicationPackage { Name = "my-app" }
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

        _ = _environmentUtilities
            .Setup(c => c.GetLastNonNotDeployedCustomerEnvironmentVersion("env"))
            .ReturnsAsync(env);
        _ = _customerPortalClientMock
            .Setup(c => c.GetCustomerEnvironmentById(env.Id, 1))
            .ReturnsAsync(env);

        return ceap;
    }
}
