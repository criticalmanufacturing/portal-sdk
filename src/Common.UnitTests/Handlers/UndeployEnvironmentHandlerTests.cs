using Moq;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.Foundation.Common.Licenses.Enums;

namespace Common.UnitTests.Handlers;

public class UndeployEnvironmentHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<IEnvironmentUtilities> _newEnvironmentUtilitiesMock = new();
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
        var env = new CustomerEnvironment { Name = "env", Id = 11, Version = 4 };
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);

        await _handler.Run("env", true);

        // assert warning is always logged
        _sessionMock.Verify(s => s.LogInformation("The Undeploy operation will uninstall the Customer Environment cleaning up all persistent resources associated with it, rendering them unrecoverable."), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("Customer environment env version 4 (Id 11) created..."), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Once);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Once);
        _customerEnvironmentServicesMock.Verify(
            s => s.TerminateOtherVersions(env, true, It.Is<bool>(b => b == true), true),
            Times.Once);
    }

    [Fact]
    public async Task Run_WithoutPurge_WipesEverythingAndLogsPurgeNotice()
    {
        // Arrange
        var env = new CustomerEnvironment { Name = "env", Id = 11, Version = 4 };
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);

        // Act
        await _handler.Run("env", true, purge: false);

        // Assert
        _sessionMock.Verify(s => s.LogInformation(UndeployEnvironmentHandler.PurgeNotice("env")), Times.Once);
        _customerEnvironmentServicesMock.Verify(s => s.TerminateOtherVersions(env, true, true, true), Times.Once);
    }

    [Fact]
    public async Task Run_WithoutPurgeArgument_LogsNoticeNamingTheCommandsThatPurge()
    {
        // Arrange
        var env = new CustomerEnvironment { Name = "my env", Id = 11, Version = 4 };
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("my env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);

        // Act: the deprecated 'undeploy -n <name>' command doesn't take --purge and calls Run without it
        await _handler.Run("my env", true);

        // Assert
        _sessionMock.Verify(s => s.LogInformation(It.Is<string>(m =>
            m.StartsWith("Notice: ")
            && m.Contains("'cmf-portal undeploy env \"my env\" --purge'")
            && m.Contains("'Reset-Environment -Name \"my env\" -Purge'")
            && m.Contains("'cmf-portal undeploy -n <name>' command doesn't take --purge"))), Times.Once);
    }

    [Theory]
    [InlineData("env", "undeploy env \"env\" --purge")]
    [InlineData(null, "undeploy env <name> --purge")]
    [InlineData(" ", "undeploy env <name> --purge")]
    public void PurgeCommandLine_QuotesTheNameOrUsesAPlaceholder(string? name, string expected)
    {
        Assert.Equal(expected, UndeployEnvironmentHandler.PurgeCommandLine(name));
    }

    [Fact]
    public async Task Run_WithPurge_WipesEverythingWithoutPurgeNotice()
    {
        // Arrange
        var env = new CustomerEnvironment { Name = "env", Id = 11, Version = 4 };
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);
        _customerEnvironmentServicesMock.Setup(s => s.CreateEnvironment(env))
            .ReturnsAsync(env);

        // Act
        await _handler.Run("env", true, purge: true);

        // Assert
        _sessionMock.Verify(s => s.LogInformation(It.Is<string>(m => m.StartsWith("Notice: "))), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.TerminateOtherVersions(env, true, true, true), Times.Once);
        _sessionMock.Verify(s => s.LogInformation("Customer environment env undeploy succeeded..."), Times.Once);
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

    [Fact]
    public async Task Run_ProdEnv_ThrowsException()
    {
        var env = new CustomerEnvironment
        {
            EnvironmentType = nameof(EnvironmentType.Production)
        };
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironment("env"))
            .ReturnsAsync(env);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Run("env", true));

        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironment("env"), Times.Once);
        _newEnvironmentUtilitiesMock.Verify(u => u.CheckEnvironmentConnection(env), Times.Never);
        _customerEnvironmentServicesMock.Verify(s => s.CreateEnvironment(env), Times.Never);
    }
}
