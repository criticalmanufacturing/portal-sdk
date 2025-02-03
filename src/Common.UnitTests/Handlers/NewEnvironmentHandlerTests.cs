using Autofac.Extras.Moq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.Foundation.Common.Licenses.Enums;
using Moq;
using System.Reflection;
using Cmf.Foundation.BusinessObjects;

namespace Common.UnitTests.Handlers;

public class NewEnvironmentHandlerTests
{
    private static string name = "name";
    private static FileInfo parameters = new(Path.Combine(
            Directory.GetParent(Assembly.GetExecutingAssembly().Location)!.FullName,
            "assets",
            "parameters.json"));
    private static EnvironmentType environmentType = EnvironmentType.Development;
    private static string siteName = "siteName";
    private static string licenseName = "licenseName";
    private static string deploymentPackageName = "deploymentPackageName";
    private static DeploymentTarget target = DeploymentTarget.KubernetesOnPremisesTarget;
    private static DirectoryInfo outputDir = new DirectoryInfo("path");
    private static string[] replaceTokens = [];
    private static bool interactive = false;
    private static string customerInfrastructureName = string.Empty;
    private static string description = "description";
    private static bool terminateOtherVersions = false;
    private static bool isInfrastructureAgent = false;
    private static double? minutesTimeoutMainTask = null;
    private static double? minutesTimeoutToGetSomeMBMsg = null;
    private static bool terminateOtherVersionsRemove = false;
    private static bool terminateOtherVersionsRemoveVolumes = false;

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistAndIsInfrastructureAgent_cedpCollectionHasNoTargetEntity()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = "UnitTest Customer Environment" };

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock.Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
                                        .Returns(Task.FromResult(customerEnvironment));

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName, target, outputDir, replaceTokens, interactive, 
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent: true, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg, 
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        var cedpCollection = new CustomerEnvironmentDeploymentPackageCollection
        {
            new CustomerEnvironmentDeploymentPackage
            {
                SourceEntity = customerEnvironment,
                TargetEntity = null,
                SoftwareLicense = null
            }
        };

        customerEnvironmentServicesMock.Verify(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(It.IsAny<CustomerEnvironment>(), 
                                                                        It.Is<CustomerEnvironmentDeploymentPackageCollection>(x => x.SoftEquals(cedpCollection))), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistAndIsNotInfrastructureAgent_cedpCollectionHasTargetEntityAndSoftwareLicense()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();

        var deploymentPackage = new DeploymentPackage();
        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        customerPortalClientMock.Setup(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>())).Returns(Task.FromResult(deploymentPackage));

        var cPSoftwareLicense = new CPSoftwareLicense() { Name = "UnitTest CP Software License" };
        var licenseServiceMock = mock.Mock<ILicenseServices>();
        licenseServiceMock.Setup(x => x.GetLicenseByUniqueName(It.IsAny<string>())).Returns(Task.FromResult(cPSoftwareLicense));

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent: false, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(It.IsAny<CustomerEnvironment>(),
                                                                        It.Is<CustomerEnvironmentDeploymentPackageCollection>(x => x.FirstOrDefault()!.TargetEntity == deploymentPackage &&
                                                                                                                                    x.FirstOrDefault()!.SoftwareLicense == cPSoftwareLicense)), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentExistsAndPassedDeploymentPackage_DeploymentPackageAndLicenseIsFetchedFromCustomerPortal()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = "UnitTest Customer Environment" };
        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock.Setup(x => x.GetCustomerEnvironment(It.IsAny<ISession>(), It.IsAny<string>())).Returns(Task.FromResult(customerEnvironment));
        customerEnvironmentServicesMock.Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
                        .Returns(Task.FromResult(customerEnvironment));

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        var licenseServiceMock = mock.Mock<ILicenseServices>();

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent: false, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(It.IsAny<CustomerEnvironment>(), It.IsAny<CustomerEnvironmentDeploymentPackageCollection>()), Times.Once);
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicenseByUniqueName(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentExistsAndPassedDeploymentPackage_DeploymentPackageAndLicenseIsNotFetchedFromCustomerPortal()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = "UnitTest Customer Environment" };
        customerEnvironment.RelationCollection = new CmfEntityRelationCollection();

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock.Setup(x => x.GetCustomerEnvironment(It.IsAny<ISession>(), It.IsAny<string>())).Returns(Task.FromResult(customerEnvironment));
        customerEnvironmentServicesMock.Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
                                .Returns(Task.FromResult(customerEnvironment));

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        var licenseServiceMock = mock.Mock<ILicenseServices>();

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName: string.Empty, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent: false, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(It.IsAny<CustomerEnvironment>(), It.IsAny<CustomerEnvironmentDeploymentPackageCollection>()), Times.Once);
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        licenseServiceMock.Verify(x => x.GetLicenseByUniqueName(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistInAnInfrastructure_LicenseDeploymentPackageAndCreationMethodCalled()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = "UnitTest Customer Environment" };
        customerEnvironment.RelationCollection = new CmfEntityRelationCollection();

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock.Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
                                .Returns(Task.FromResult(customerEnvironment));

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        var licenseServiceMock = mock.Mock<ILicenseServices>();

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName: "infrastructureName", description, terminateOtherVersions, isInfrastructureAgent: false, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicenseByUniqueName(It.IsAny<string>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.CreateCustomerEnvironmentForCustomerInfrastructure(It.IsAny<CustomerEnvironment>(), It.IsAny<string>(), 
                                                                                It.IsAny<bool>(), It.IsAny<CustomerEnvironmentDeploymentPackageCollection>()), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentExistsInAnInfrastructure_LicenseDeploymentPackageAndCreationMethodCalled()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = "UnitTest Customer Environment" };
        customerEnvironment.RelationCollection = new CmfEntityRelationCollection();

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        var licenseServiceMock = mock.Mock<ILicenseServices>();

        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();

        // Act
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseName, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName: "infrastructureName", description, terminateOtherVersions, isInfrastructureAgent: false, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicenseByUniqueName(It.IsAny<string>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.CreateCustomerEnvironmentForCustomerInfrastructure(It.IsAny<CustomerEnvironment>(), It.IsAny<string>(),
                                                                                It.IsAny<bool>(), It.IsAny<CustomerEnvironmentDeploymentPackageCollection>()), Times.Once);
    }
}
