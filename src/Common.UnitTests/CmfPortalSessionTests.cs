using Cmf.CustomerPortal.Sdk.Common;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;

namespace Common.UnitTests;

[CollectionDefinition("Token environment", DisableParallelization = true)]
public class TokenEnvironmentCollection;

[Collection("Token environment")]
public class CmfPortalSessionTests : IDisposable
{
    private readonly string? originalToken = Environment.GetEnvironmentVariable("CM_PORTAL_TOKEN");
    private static readonly string TokenPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cmfportal", "cmfportaltoken");

    public CmfPortalSessionTests()
    {
        Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", null);
    }

    [Fact]
    public void GetToken_PrefersEnvironmentOverSavedLogin()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", "test-environment-token");
        var files = new MockFileSystem();
        files.AddFile(TokenPath, new MockFileData("test-saved-token"));
        var session = new TestSession(files);

        // Act
        string token = session.GetToken();

        // Assert
        Assert.Equal("test-environment-token", token);
        Assert.Equal("test-saved-token", files.File.ReadAllText(TokenPath));
        Assert.DoesNotContain(session.Messages, message => message.Contains(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetToken_UsesSavedLoginWhenEnvironmentIsEmpty(string? environmentToken)
    {
        // Arrange
        Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", environmentToken);
        var files = new MockFileSystem();
        files.AddFile(TokenPath, new MockFileData("test-saved-token"));
        var session = new TestSession(files);

        // Act
        string token = session.GetToken();

        // Assert
        Assert.Equal("test-saved-token", token);
        Assert.DoesNotContain(session.Messages, message => message.Contains(token));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n ")]
    public void GetToken_ThrowsWhenNoTokenIsAvailable(string? savedToken)
    {
        // Arrange
        var files = new MockFileSystem();
        if (savedToken != null)
        {
            files.AddFile(TokenPath, new MockFileData(savedToken));
        }
        var session = new TestSession(files);
        string[] originalFiles = files.AllFiles.ToArray();
        string[] originalDirectories = files.AllDirectories.ToArray();

        // Act
        var exception = Assert.Throws<InvalidOperationException>(() => session.GetToken());

        // Assert
        Assert.Contains("log in", exception.Message);
        Assert.Equal(originalFiles, files.AllFiles);
        Assert.Equal(originalDirectories, files.AllDirectories);
    }

    public void Dispose() => Environment.SetEnvironmentVariable("CM_PORTAL_TOKEN", originalToken);

    private sealed class TestSession(IFileSystem fileSystem) : CmfPortalSession(fileSystem)
    {
        public List<string> Messages { get; } = [];

        public override void LogDebug(string message) => Messages.Add(message);
        public override void LogError(string message) => Messages.Add(message);
        public override void LogError(Exception exception) => Messages.Add(exception.Message);
        public override void LogInformation(string message) => Messages.Add(message);
        public override void LogPendingMessages() { }
    }
}
