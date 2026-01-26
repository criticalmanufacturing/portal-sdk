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

        testOutputHelper.WriteLine(string.Join('\n', Assembly.GetExecutingAssembly().GetManifestResourceNames()));
        // Since we are mocking the filesystem, we have to add our *real* asset into it
        fileSystem.AddFileFromEmbeddedResource(
            attachmentFilePath,
            Assembly.GetExecutingAssembly(),
            "Common.UnitTests.assets.archive.zip"
        );
        
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

    [Fact]
    public async void Check_GeneratedPrefix_App()
    {
        // Arrange
        var appPackage = new CustomerEnvironmentApplicationPackage()
        {
            Name = "TestAppPackage",
            Id = 1,
            TargetEntity = new ApplicationPackage()
            {
                Name = "TestApp",
                Id = 2
            }
        };

        string expectedFilename = $"App_{appPackage.TargetEntity.Name}";

        var ed = new EntityDocumentation
        {
            Filename = expectedFilename,
            Id = 30,
            CreatedOn = new DateTime(2025, 1, 1)
        };


        string attachmentFilePath = Path.Combine(
            Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
            "assets",
            "archive.zip");
        var entityDocumentationCollection = new EntityDocumentationCollection { ed };

        var customerPortalClientMock = new Mock<ICustomerPortalClient>();
        customerPortalClientMock.Setup(x => x.GetAttachmentsForEntity(It.IsAny<EntityBase>()))
            .ReturnsAsync(entityDocumentationCollection);
        customerPortalClientMock.Setup(x => x.DownloadAttachmentStreaming(It.IsAny<long>()))
            .ReturnsAsync(attachmentFilePath);

        var sessionMock = new Mock<ISession>();

        var fileSystem = new MockFileSystem();

        // Since we are mocking the filesystem, we have to add our *real* asset into it
        fileSystem.AddFileFromEmbeddedResource(
            attachmentFilePath,
            Assembly.GetExecutingAssembly(),
            "Common.UnitTests.assets.archive.zip"
        );

        var artifactsDownloaderHandler =
            new ArtifactsDownloaderHandler(sessionMock.Object, fileSystem, customerPortalClientMock.Object);

        // Act
        const string outputPath = "/some/fake/directory";
        bool result = await artifactsDownloaderHandler.Handle(appPackage, outputPath);

        // Assert

        // verify the correct log message with expected filename
        sessionMock.Verify(x => x.LogDebug($"Downloading attachment {expectedFilename}"), Times.Once);

        // verify execution continues as expected with the correct ed.Id
        customerPortalClientMock.Verify(x => x.DownloadAttachmentStreaming(ed.Id), Times.Once);
    }
}
