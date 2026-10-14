using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;
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
    private readonly MockFileSystem _fileSystem = new();

    private readonly GetEnvironmentLogsHandler _handler;

    public GetEnvironmentLogsHandlerTests()
    {
        _handler = new GetEnvironmentLogsHandler(_sessionMock.Object, _customerPortalClientMock.Object, _customerEnvironmentServicesMock.Object, _fileSystem);

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

    [Fact]
    public async Task WriteLogs_ToAFilePath_WritesTheIndentedJsonCreatingMissingDirectories()
    {
        // Arrange
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.All);
        string expectedPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("out", "logs", "my-logs.json"));

        // Act
        string path = _handler.WriteLogs(logs, _fileSystem.Path.Combine("out", "logs", "my-logs.json"), "env", null);

        // Assert
        Assert.Equal(expectedPath, path);
        string written = _fileSystem.File.ReadAllText(path);
        Assert.True(JsonNode.DeepEquals(logs, JsonNode.Parse(written)));
        Assert.Contains(Environment.NewLine + "  \"installation\"", written);
        _sessionMock.Verify(s => s.LogInformation($"Logs written to {expectedPath}"), Times.Once);
    }

    [Theory]
    [InlineData(null, "env-logs.json")]
    [InlineData(3, "env-v3-logs.json")]
    public async Task WriteLogs_ToAnExistingDirectory_NamesTheFileAfterTheEnvironment(int? version, string expectedFileName)
    {
        // Arrange
        JsonObject logs = await _handler.Run("env", version, EnvironmentLogType.All);
        _fileSystem.Directory.CreateDirectory("out");

        // Act
        string path = _handler.WriteLogs(logs, "out", "env", version);

        // Assert
        Assert.Equal(_fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("out", expectedFileName)), path);
        Assert.True(_fileSystem.File.Exists(path));
    }

    [Fact]
    public async Task WriteLogs_ToAPathEndingInASeparator_CreatesTheDirectoryAndNamesTheFileAfterTheEnvironment()
    {
        // Arrange
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.All);

        // Act
        string path = _handler.WriteLogs(logs, "new-dir" + _fileSystem.Path.DirectorySeparatorChar, "env", null);

        // Assert
        Assert.Equal(_fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("new-dir", "env-logs.json")), path);
        Assert.True(_fileSystem.File.Exists(path));
    }

    [Fact]
    public async Task WriteLogs_ToAnExistingFile_OverwritesIt()
    {
        // Arrange
        JsonObject logs = await _handler.Run("env", null, EnvironmentLogType.Termination);
        _fileSystem.AddFile("logs.json", new MockFileData("old content"));

        // Act
        string path = _handler.WriteLogs(logs, "logs.json", "env", null);

        // Assert
        Assert.True(JsonNode.DeepEquals(logs, JsonNode.Parse(_fileSystem.File.ReadAllText(path))));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WriteLogs_WhenOutputIsNullOrWhitespace_ThrowsArgumentException(string? output)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => _handler.WriteLogs(new JsonObject(), output!, "env", null));
        Assert.Empty(_fileSystem.AllFiles);
    }
}
