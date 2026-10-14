using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using System.Text.Json.Nodes;

namespace Common.UnitTests.Handlers;

public class GetEnvironmentLogsHandlerTests
{
    private const long EnvironmentId = 7;
    private const string InstallationLogs = """[{"StepId":"Pre","Data":"Installing"}]""";
    private const string TerminationLogs = """[{"StepId":"Pre","Data":"Terminating"}]""";

    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<ICustomerEnvironmentServices> _customerEnvironmentServicesMock = new();

    private readonly GetEnvironmentLogsHandler _handler;

    public GetEnvironmentLogsHandlerTests()
    {
        _handler = new GetEnvironmentLogsHandler(_sessionMock.Object, _customerPortalClientMock.Object, _customerEnvironmentServicesMock.Object);

        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironmentVersion("env", It.IsAny<int?>()))
            .ReturnsAsync(new CustomerEnvironment { Id = EnvironmentId, Name = "env" });
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentDeploymentLogs(EnvironmentId)).ReturnsAsync(InstallationLogs);
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentTerminationLogs(EnvironmentId)).ReturnsAsync(TerminationLogs);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _handler.Run(name!, null, EnvironmentLogType.All));
        _customerPortalClientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Run_WithAll_ReturnsInstallationAndTerminationLogsOfTheRequestedVersion()
    {
        // Act
        JsonObject logs = await _handler.Run("env", 3, EnvironmentLogType.All);

        // Assert
        Assert.Equal(["installation", "termination"], logs.Select(property => property.Key));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(InstallationLogs), logs["installation"]));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(TerminationLogs), logs["termination"]));
        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironmentVersion("env", 3), Times.Once);
    }

    [Fact]
    public async Task Run_WithInstallation_ReturnsOnlyTheInstallationLogs()
    {
        // Act
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.Installation);

        // Assert
        Assert.Equal("installation", Assert.Single(logs).Key);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentTerminationLogs(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_WithTermination_ReturnsOnlyTheTerminationLogs()
    {
        // Act
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.Termination);

        // Assert
        Assert.Equal("termination", Assert.Single(logs).Key);
        _customerPortalClientMock.Verify(c => c.GetCustomerEnvironmentDeploymentLogs(It.IsAny<long>()), Times.Never);
    }

    [Fact]
    public async Task Run_KeepsLogsThatAreNotJsonAsStringsAndMissingLogsAsNull()
    {
        // Arrange
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentDeploymentLogs(EnvironmentId)).ReturnsAsync("plain text log");
        _customerPortalClientMock.Setup(c => c.GetCustomerEnvironmentTerminationLogs(EnvironmentId)).ReturnsAsync((string)null!);

        // Act
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.All);

        // Assert
        Assert.Equal("plain text log", logs["installation"]!.GetValue<string>());
        Assert.True(logs.ContainsKey("termination"));
        Assert.Null(logs["termination"]);
    }

    [Fact]
    public async Task Run_WhenVersionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironmentVersion("env", 3))
            .ThrowsAsync(new NotFoundException("not found"));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Run("env", 3, EnvironmentLogType.All));
        _customerPortalClientMock.VerifyNoOtherCalls();
    }
}
