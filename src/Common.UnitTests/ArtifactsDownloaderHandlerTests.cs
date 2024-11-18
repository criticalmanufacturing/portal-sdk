using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using System.Reflection;

namespace Common.UnitTests;

public class ArtifactsDownloaderHandlerTests
{
    [Fact]
    public async void Handle_OutputPathSet_AttachmentIsDownloadedToOutputPath()
    {
        // Arrange
        var customerEnvironment = new CustomerEnvironment()
        {
            Name = "test",
            Id = 25
        };
        var entityDocumentation = new EntityDocumentation()
        {
            Filename = $"CustomerEnvironment_{customerEnvironment.Name}",
            Id = 30
        };
        string attachmentFilePath = Path.Combine(
            Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
            "assets",
            "archive.zip");
        var entityDocumentationCollection = new EntityDocumentationCollection() { entityDocumentation };
        var customerPortalClientMock = new Mock<ICustomerPortalClient>();
        customerPortalClientMock.Setup(x => x.GetAttachmentsForEntity(customerEnvironment)).ReturnsAsync(entityDocumentationCollection);
        customerPortalClientMock.Setup(x => x.DownloadAttachmentStreaming(entityDocumentation.Id)).ReturnsAsync(attachmentFilePath);
        var sessionMock = new Mock<ISession>();
        var artifactsDownloaderHandler = new ArtifactsDownloaderHandler(sessionMock.Object, customerPortalClientMock.Object);
        string outputPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        bool result = await artifactsDownloaderHandler.Handle(customerEnvironment, outputPath);

        // Assert
        Assert.True(result);
        Assert.Single(Directory.GetFiles(outputPath));
        Assert.Equal("checksums.txt", Path.GetFileName(Directory.GetFiles(outputPath)[0]));
    }
}