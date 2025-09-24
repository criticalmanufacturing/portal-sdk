using System.IO.Abstractions.TestingHelpers;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Moq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.Foundation.BusinessObjects;
using System.Reflection;
using Xunit.Abstractions;

namespace Common.UnitTests;

public class ArtifactsDownloaderHandlerTests(ITestOutputHelper testOutputHelper)
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
        var newerEd = new EntityDocumentation
        {
            Filename = $"CustomerEnvironment_{customerEnvironment.Name}",
            Id = 30,
            CreatedOn = new DateTime(2025, 1, 1)
        };

        var olderEd = new EntityDocumentation
        {
            Filename = $"CustomerEnvironment_{customerEnvironment.Name}",
            Id = 10,
            CreatedOn = new DateTime(2000, 1, 1)
        };
        string attachmentFilePath = Path.Combine(
            Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
            "assets",
            "archive.zip");
        var entityDocumentationCollection = new EntityDocumentationCollection { newerEd, olderEd };

        var customerPortalClientMock = new Mock<ICustomerPortalClient>();
        customerPortalClientMock.Setup(x => x.GetAttachmentsForEntity(It.IsAny<EntityBase>()))
            .ReturnsAsync(entityDocumentationCollection);
        customerPortalClientMock.Setup(x => x.DownloadAttachmentStreaming(It.IsAny<long>()))
            .ReturnsAsync(attachmentFilePath);

        var sessionMock = new Mock<ISession>();

        var fileSystem = new MockFileSystem();
        
        // Since we are mocking the filesystem, we have to copy our *real* asset into it
        fileSystem.AddFile(attachmentFilePath, new MockFileData(await File.ReadAllBytesAsync(attachmentFilePath)));

        var artifactsDownloaderHandler =
            new ArtifactsDownloaderHandler(sessionMock.Object, fileSystem, customerPortalClientMock.Object);

        // Act
        const string outputPath = "/some/fake/directory";
        bool result = await artifactsDownloaderHandler.Handle(customerEnvironment, outputPath);

        // Assert

        // 1 call to passed in environment
        customerPortalClientMock.Verify(x => x.GetAttachmentsForEntity(customerEnvironment), Times.Once);

        // 1 call with newest EntityDocumentation
        customerPortalClientMock.Verify(x => x.DownloadAttachmentStreaming(newerEd.Id), Times.Once);

        customerPortalClientMock.VerifyNoOtherCalls();

        Assert.True(result);
        var extractedFiles = fileSystem.DirectoryInfo.New(outputPath).GetFiles();
        Assert.Single(extractedFiles);

        var file = extractedFiles.First();
        Assert.Equal("checksums.txt", file.Name);
        Assert.Equal(731, file.Length);

        testOutputHelper.WriteLine(file.FullName);
    }
}
