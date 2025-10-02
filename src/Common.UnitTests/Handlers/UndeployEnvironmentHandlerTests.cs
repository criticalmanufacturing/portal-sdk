using Moq;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Base;

namespace Common.UnitTests.Handlers;

public class UndeployEnvironmentHandlerTests
{
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<INewEnvironmentUtilities> _newEnvironmentUtilitiesMock = new();
    private readonly Mock<IEnvironmentDeploymentHandler> _environmentDeploymentHandlerMock = new();
    private readonly Mock<ICustomerEnvironmentServices> _customerEnvironmentServicesMock = new();

    private readonly UndeployEnvironmentHandler _handler;

    public UndeployEnvironmentHandlerTests()
    {
        _handler = new UndeployEnvironmentHandler(
            _sessionMock.Object,
            _newEnvironmentUtilitiesMock.Object,
            _customerEnvironmentServicesMock.Object);
    }

    [Fact]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentNullException()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(null, false));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(" ", false));
    }

    [Fact]
    public async Task Run_WhenEnvironmentDoesNotExist_ThrowsException()
    {
        _ = _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync((CustomerEnvironment?)null);

        await Assert.ThrowsAsync<Exception>(() => _handler.Run("env", false));
    }

    [Fact]
    public async Task Run_WhenValid_RunsSuccessfully()
    {
        var env = new CustomerEnvironment();
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);

        await _handler.Run("env", true);

        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.TerminateOtherVersions(env, true, true), Times.Once);
    }
}
