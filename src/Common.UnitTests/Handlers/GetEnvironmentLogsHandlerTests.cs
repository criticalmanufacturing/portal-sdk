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
    public void WriteLogs_ToAFilePath_WritesTheContentsCreatingMissingDirectories()
    {
        // Arrange
        string expectedPath = _fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("out", "logs", "my-logs.json"));

        // Act
        string path = _handler.WriteLogs("the logs", _fileSystem.Path.Combine("out", "logs", "my-logs.json"), "env", null, ".json");

        // Assert
        Assert.Equal(expectedPath, path);
        Assert.Equal("the logs", _fileSystem.File.ReadAllText(path));
        _sessionMock.Verify(s => s.LogInformation($"Logs written to {expectedPath}"), Times.Once);
    }

    [Theory]
    [InlineData(null, ".json", "env-logs.json")]
    [InlineData(3, ".json", "env-v3-logs.json")]
    [InlineData(null, ".log", "env-logs.log")]
    [InlineData(3, ".log", "env-v3-logs.log")]
    public void WriteLogs_ToAnExistingDirectory_NamesTheFileAfterTheEnvironment(int? version, string extension, string expectedFileName)
    {
        // Arrange
        _fileSystem.Directory.CreateDirectory("out");

        // Act
        string path = _handler.WriteLogs("the logs", "out", "env", version, extension);

        // Assert
        Assert.Equal(_fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("out", expectedFileName)), path);
        Assert.Equal("the logs", _fileSystem.File.ReadAllText(path));
    }

    [Fact]
    public void WriteLogs_ToAPathEndingInASeparator_CreatesTheDirectoryAndNamesTheFileAfterTheEnvironment()
    {
        // Act
        string path = _handler.WriteLogs("the logs", "new-dir" + _fileSystem.Path.DirectorySeparatorChar, "env", null, ".log");

        // Assert
        Assert.Equal(_fileSystem.Path.GetFullPath(_fileSystem.Path.Combine("new-dir", "env-logs.log")), path);
        Assert.True(_fileSystem.File.Exists(path));
    }

    [Fact]
    public void WriteLogs_ToAnExistingFile_OverwritesIt()
    {
        // Arrange
        _fileSystem.AddFile("logs.json", new MockFileData("old content"));

        // Act
        string path = _handler.WriteLogs("the logs", "logs.json", "env", null, ".json");

        // Assert
        Assert.Equal("the logs", _fileSystem.File.ReadAllText(path));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void WriteLogs_WhenOutputIsNullOrWhitespace_ThrowsArgumentException(string? output)
    {
        // Act & Assert
        Assert.ThrowsAny<ArgumentException>(() => _handler.WriteLogs("the logs", output!, "env", null, ".json"));
        Assert.Empty(_fileSystem.AllFiles);
    }

    [Fact]
    public void WriteLogs_WhenContentsAreNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.WriteLogs(null!, "logs.json", "env", null, ".json"));
        Assert.Empty(_fileSystem.AllFiles);
    }
}
