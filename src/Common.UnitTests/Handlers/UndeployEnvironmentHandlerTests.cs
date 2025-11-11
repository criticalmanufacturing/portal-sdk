using Moq;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;

namespace Common.UnitTests.Handlers;

public class UndeployEnvironmentHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<INewEnvironmentUtilities> _newEnvironmentUtilitiesMock = new();
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
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(null, true));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run("", true));
        await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Run(" ", true));

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment(It.IsAny<string>()), Times.Never);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(It.IsAny<CustomerEnvironment>()), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(It.IsAny<CustomerEnvironment>()), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.TerminateOtherVersions(It.IsAny<CustomerEnvironment>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
    }

    [Fact]
    public async Task Run_WhenEnvironmentDoesNotExist_ThrowsException()
    {
        _ = _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync((CustomerEnvironment?)null);

        await Assert.ThrowsAsync<Exception>(() => _handler.Run("env", true));

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Once);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(It.IsAny<CustomerEnvironment>()), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(It.IsAny<CustomerEnvironment>()), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.TerminateOtherVersions(It.IsAny<CustomerEnvironment>(), It.IsAny<bool>(), It.IsAny<bool>(), It.IsAny<bool>()), Times.Never);
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

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Once);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(
            s => s.TerminateOtherVersions(env, true, It.Is<bool>(b => b == true), true),
            Times.Once);
    }

    [Fact]
    public async Task Run_WhenForceDisabledAndNegativeInput_Cancels()
    {
        var env = new CustomerEnvironment();
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);
        Console.SetIn(new StringReader("a"));

        await _handler.Run("env", false);

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Never);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Never);
    }

    [Fact]
    public async Task Run_WhenForceDisabledAndPositiveInput_RunsSuccessfully()
    {
        var env = new CustomerEnvironment();
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);
        Console.SetIn(new StringReader("y"));

        await _handler.Run("env", false);

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Once);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(
            s => s.TerminateOtherVersions(env, true, It.Is<bool>(b => b == true), true),
            Times.Once);
    }
}
