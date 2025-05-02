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
    [Fact]
    public async void Run_PackageDoesNotExistInPortal_PackageIsUploaded()
    {
        // arrange
        var customerPortalClientMock = new Mock<ICustomerPortalClient>();
        var sessionMock = new Mock<ISession>();
        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        var publishPackageHandler = new PublishPackageHandler(customerPortalClientMock.Object, sessionMock.Object, queryProxyServiceMock.Object);
        var fileSystemInfo = new FileInfo(Path.Combine(
                                        Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                                        "assets",
                                        "Cmf.Environment.11.2.0-alpha.1.zip"));

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
        var customerPortalClientMock = new Mock<ICustomerPortalClient>();
        var sessionMock = new Mock<ISession>();
        var output = new ExecuteQueryOutput();
        output.TotalRows = 1;
        var queryProxyServiceMock = new Mock<IQueryProxyService>();
        queryProxyServiceMock.Setup(x => x.ExecuteQuery(It.IsAny<QueryObject>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<ISession>()))
            .ReturnsAsync(() => output);
        var publishPackageHandler = new PublishPackageHandler(customerPortalClientMock.Object, sessionMock.Object, queryProxyServiceMock.Object);
        var fileSystemInfo = new FileInfo(Path.Combine(
                                Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
                                "assets",
                                "Cmf.Environment.11.2.0-alpha.1.zip"));

        // act
        await publishPackageHandler.Run(fileSystemInfo, "");

        // assert
        sessionMock.Verify(x => x.LogInformation($"Package {fileSystemInfo.Name} skipped"));
    }
}
