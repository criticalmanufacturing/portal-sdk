using System.IO.Abstractions.TestingHelpers;
using System.Reflection;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.OutputObjects;
using Moq;

namespace Common.UnitTests.Handlers;

public class PublishPackageHandlerTests
{
    private const string _assets = "assets";
    private const string _testPackage = "Cmf.Environment.11.2.0-alpha.1.zip";

    private readonly string _validPackageResourcePath =
        $"{Assembly.GetExecutingAssembly().GetName().Name}.{_assets}.{_testPackage}";

    [Theory]
    [InlineData("Cmf.Environment.11.2.zip")]
    [InlineData("Cmf.Environment-11.2-alpha.1.zip")]
    [InlineData("Cmf.Environment.11.2.0-.zip")]
    [InlineData("Cmf.Environment.11.2.0.rc1.zip")]
    public async void Run_PackageHasWrongFormat_PackageIsNotUploaded(string packageName)
    {
        // arrange
        var sessionMock = new Mock<ISession>();
        var queryProxyServiceMock = new Mock<IQueryProxyService>();

        var fileSystem = new MockFileSystem();
        var filePath = fileSystem.Path.Combine("/whatever/", packageName);
        fileSystem.AddFileFromEmbeddedResource(
            filePath, // valid data, invalid name
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );

        var publishPackageHandler = new PublishPackageHandler(
            sessionMock.Object,
            fileSystem,
            queryProxyServiceMock.Object
        );

        // act
        await publishPackageHandler.Run(filePath, "");

        // assert
        sessionMock.Verify(x => x.LogInformation($"Package {packageName} skipped"));
    }

    [Fact]
    public async void Run_PackageDoesNotExistInPortal_PackageIsUploaded()
    {
        // arrange
        var sessionMock = new Mock<ISession>();
        var queryProxyServiceMock = new Mock<IQueryProxyService>();

        var fileSystem = new MockFileSystem();
        var packagePath = Path.Combine("/some/path", _testPackage);
        fileSystem.AddFileFromEmbeddedResource(
            packagePath,
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );

        var publishPackageHandler = new PublishPackageHandler(
            sessionMock.Object,
            fileSystem,
            queryProxyServiceMock.Object
        );


        // act
        await publishPackageHandler.Run(packagePath, "");

        // assert
        sessionMock.Verify(x => x.LogDebug("Package does not exist"));
        sessionMock.Verify(x => x.LogDebug("Uploading package..."));
    }

    [Fact]
    public async void Run_PackageExistsInPortal_PackageIsNotUploaded()
    {
        // arrange
        var sessionMock = new Mock<ISession>();

        var fileSystem = new MockFileSystem();
        var packagePath = Path.Combine("/some/path", _testPackage);
        fileSystem.AddFileFromEmbeddedResource(
            packagePath,
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );

        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        queryProxyServiceMock.Setup(x => x.ExecuteQuery(
                    It.IsAny<QueryObject>(),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<ISession>()
                )
            )
            .ReturnsAsync(() => new ExecuteQueryOutput { TotalRows = 1 });
        var publishPackageHandler = new PublishPackageHandler(
            sessionMock.Object,
            fileSystem,
            queryProxyServiceMock.Object
        );

        // act
        await publishPackageHandler.Run(packagePath, "");

        // assert
        sessionMock.Verify(x => x.LogInformation($"Package {_testPackage} skipped"));
    }

    [Fact]
    public async Task Run_PassingDirectoryTriesAllPackages()
    {
        // Arrange
        var sessionMock = new Mock<ISession>();

        var fileSystem = new MockFileSystem();
        const string packagesDir = "/some/dir";
        fileSystem.AddFileFromEmbeddedResource(
            fileSystem.Path.Combine(packagesDir, "Cmf.Environment.11.2.0.zip"),
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );
        const string existingPkgName = "Cmf.Host";
        const string existingPkg = $"{existingPkgName}.11.2.0.zip";
        fileSystem.AddFileFromEmbeddedResource(
            fileSystem.Path.Combine(packagesDir, existingPkg),
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );
        fileSystem.AddFileFromEmbeddedResource(
            fileSystem.Path.Combine(packagesDir, "Cmf.Security.11.2.0.zip"),
            Assembly.GetExecutingAssembly(),
            _validPackageResourcePath
        );

        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        queryProxyServiceMock.Setup(x => x.ExecuteQuery(
                    It.Is<QueryObject>(qo =>
                        qo.Query.Filters.Exists(f => "Name".Equals(f.Name) && existingPkgName.Equals(f.Value))
                    ),
                    It.IsAny<int>(),
                    It.IsAny<int>(),
                    It.IsAny<ISession>()
                )
            )
            .ReturnsAsync(() => new ExecuteQueryOutput { TotalRows = 1 });

        var handler = new PublishPackageHandler(sessionMock.Object, fileSystem, queryProxyServiceMock.Object);

        // Act
        await handler.Run(packagesDir, "");


        // Assert

        // Two of them will be uploaded
        sessionMock.Verify(x => x.LogDebug("Uploading package..."), Times.Exactly(2));

        // The one that already exists won't
        sessionMock.Verify(x => x.LogInformation($"Package {existingPkg} skipped"), Times.Once);
    }
}
