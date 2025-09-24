using System.IO.Abstractions;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects.QueryObject;
using Cmf.Foundation.BusinessOrchestration.QueryManagement.OutputObjects;
using Moq;
using System.Reflection;

namespace Common.UnitTests.Handlers;

public class PublishPackageHandlerTests
{
    private readonly string _assets = "assets";
    private readonly string _testPackage = "Cmf.Environment.11.2.0-alpha.1.zip";

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
        var fileSystem = new FileSystem(); // use real fs
        var publishPackageHandler = new PublishPackageHandler(sessionMock.Object, fileSystem, queryProxyServiceMock.Object);
        var fileSystemInfo = new FileInfo(Path.Combine(
                                        Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                                        _assets,
                                        packageName));

        // act
        await publishPackageHandler.Run(fileSystemInfo, "");

        // assert
        sessionMock.Verify(x => x.LogInformation($"Package {fileSystemInfo.Name} skipped"));
    }

    [Fact]
    public async void Run_PackageDoesNotExistInPortal_PackageIsUploaded()
    {
        // arrange
        var sessionMock = new Mock<ISession>();
        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        var fileSystem = new FileSystem(); // use real fs
        var publishPackageHandler = new PublishPackageHandler(sessionMock.Object, fileSystem, queryProxyServiceMock.Object);
        var fileSystemInfo = new FileInfo(Path.Combine(
                                        Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                                        _assets,
                                        _testPackage));

        // act
        await publishPackageHandler.Run(fileSystemInfo, "");

        // assert
        sessionMock.Verify(x => x.LogDebug("Package does not exist"));
        sessionMock.Verify(x => x.LogDebug("Uploading package..."));
    }

    [Fact]
    public async void Run_PackageExistsInPortal_PackageIsNotUploaded()
    {
        // arrange
        var sessionMock = new Mock<ISession>();
        var fileSystem = new FileSystem(); // use real fs
        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        queryProxyServiceMock.Setup(x => x.ExecuteQuery(It.IsAny<QueryObject>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ISession>()))
            .ReturnsAsync(() => new ExecuteQueryOutput
            {
                TotalRows = 1
            });
        var publishPackageHandler = new PublishPackageHandler(sessionMock.Object, fileSystem, queryProxyServiceMock.Object);
        var fileSystemInfo = new FileInfo(Path.Combine(
                                Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                                _assets,
                                _testPackage));

        // act
        await publishPackageHandler.Run(fileSystemInfo, "");

        // assert
        sessionMock.Verify(x => x.LogInformation($"Package {fileSystemInfo.Name} skipped"));
    }
}
