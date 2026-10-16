using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using System.IO.Abstractions.TestingHelpers;

namespace Common.UnitTests.Handlers;

public class DownloadArtifactsHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();
    private readonly Mock<IArtifactsDownloaderHandler> _artifactsDownloaderHandlerMock = new();

    private readonly DownloadArtifactsHandler _handler;

    public DownloadArtifactsHandlerTests()
    {
        _handler = new DownloadArtifactsHandler(
            _sessionMock.Object,
            new MockFileSystem(),
            _customerPortalClientMock.Object,
            _artifactsDownloaderHandlerMock.Object);
    }

    [Fact]
    public async Task Run_DownloadsTheArtifactsAndLogsTheEnvironmentVersion()
    {
        // Arrange
        var environment = new CustomerEnvironment { Name = "env", Id = 7, Version = 3 };
        var outputDir = new DirectoryInfo("out");
        _customerPortalClientMock.Setup(c => c.GetObjectByName<CustomerEnvironment>("env", 0))
            .ReturnsAsync(environment);
        _artifactsDownloaderHandlerMock.Setup(h => h.Handle(environment, outputDir.FullName))
            .ReturnsAsync(true);

        // Act
        await _handler.Run("env", outputDir);

        // Assert
        _sessionMock.Verify(s => s.LogInformation("Customer environment env exists, current version 3 (Id 7)..."), Times.Once);
        _artifactsDownloaderHandlerMock.Verify(h => h.Handle(environment, outputDir.FullName), Times.Once);
    }
}
