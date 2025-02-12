using Autofac.Extras.Moq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.Common.Licenses.Enums;
using Moq;

namespace Common.UnitTests.Handlers;

public class NewEnvironmentHandlerTests
{
    private static readonly string name = "name";
    private static readonly FileInfo? parameters = null;
    private static readonly EnvironmentType environmentType = EnvironmentType.Development;
    private static readonly string siteName = "siteName";
    private static readonly string licenseName = "licenseName";
    private static readonly string deploymentPackageName = "deploymentPackageName";
    private static readonly DeploymentTarget target = DeploymentTarget.KubernetesOnPremisesTarget;
    private static readonly DirectoryInfo outputDir = new DirectoryInfo("path");
    private static readonly string[] replaceTokens = [];
    private static readonly bool interactive = false;
    private static readonly string customerInfrastructureName = string.Empty;
    private static readonly string description = "description";
    private static readonly bool terminateOtherVersions = false;
    private static readonly bool isInfrastructureAgent = false;
    private static readonly double? minutesTimeoutMainTask = null;
    private static readonly double? minutesTimeoutToGetSomeMBMsg = null;
    private static readonly bool terminateOtherVersionsRemove = false;
    private static readonly bool terminateOtherVersionsRemoveVolumes = false;

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistAndIsInfrastructureAgent_DeploymentPackageIsNotUpdated()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = name };

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock
            .Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
            .Returns(Task.FromResult(customerEnvironment));

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        var licenseServiceMock = mock.Mock<ILicenseServices>();

        // Act
        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, [licenseName], deploymentPackageName, target, outputDir, replaceTokens, interactive, 
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent: true, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg, 
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(customerEnvironment), Times.Never);
        
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(deploymentPackageName, 0), Times.Never);
        licenseServiceMock.Verify(x => x.GetLicensesByUniqueName(It.IsAny<string[]>()), Times.Never);
        customerEnvironmentServicesMock.Verify(x => x.UpdateDeploymentPackage(It.IsAny<CustomerEnvironment>(), It.IsAny<DeploymentPackage>(), It.IsAny<long[]>()), Times.Never);
    }

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistAndIsNotInfrastructureAgent_DeploymentPackageIsUpdated()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = name };
        var deploymentPackage = new DeploymentPackage() { Name = deploymentPackageName };
        var cPSoftwareLicense = new CPSoftwareLicense() { Name = licenseName, Id = 1234 };
        string[] licenseNames = [licenseName];
        List<CPSoftwareLicense> licenses = [cPSoftwareLicense];

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock
            .Setup(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()))
            .Returns(Task.FromResult(customerEnvironment));
        customerEnvironmentServicesMock
            .Setup(x => x.UpdateEnvironment(customerEnvironment))
            .Returns(Task.FromResult(customerEnvironment));

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        customerPortalClientMock
            .Setup(x => x.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.FromResult(deploymentPackage));

        var licenseServiceMock = mock.Mock<ILicenseServices>();
        licenseServiceMock.Setup(x => x.GetLicensesByUniqueName(licenseNames)).Returns(Task.FromResult(licenses.AsEnumerable()));

        // Act
        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseNames, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName, description, terminateOtherVersions, isInfrastructureAgent, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.CreateEnvironment(It.IsAny<ICustomerPortalClient>(), It.IsAny<CustomerEnvironment>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(customerEnvironment), Times.Never);
        
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(deploymentPackage.Name, 0), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicensesByUniqueName(licenseNames), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateDeploymentPackage(customerEnvironment, deploymentPackage,
            It.Is<long[]>(
                x => x.Count() == 1 && x[0] == cPSoftwareLicense.Id
            )), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentDoesNotExistInAnInfrastructure_DeploymentPackageIsUpdated()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = name };
        var deploymentPackage = new DeploymentPackage() { Name = deploymentPackageName };
        var cPSoftwareLicense = new CPSoftwareLicense() { Name = licenseName, Id = 1234 };
        string[] licenseNames = [licenseName];
        List<CPSoftwareLicense> licenses = [cPSoftwareLicense];
        var ciName = "infrastructure Name";

        var newEnvironmentUtilitiesMock = mock.Mock<INewEnvironmentUtilities>();

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock
            .Setup(x => x.CreateCustomerEnvironmentForCustomerInfrastructure(It.IsAny<CustomerEnvironment>(), ciName, false))
            .Returns(Task.FromResult(customerEnvironment));

        
        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        customerPortalClientMock
            .Setup(x => x.GetObjectByName<DeploymentPackage>(deploymentPackage.Name, 0))
            .Returns(Task.FromResult(deploymentPackage));

        var licenseServiceMock = mock.Mock<ILicenseServices>();
        licenseServiceMock
            .Setup(x => x.GetLicensesByUniqueName(licenseNames))
            .Returns(Task.FromResult(licenses.AsEnumerable()));

        // Act
        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseNames, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName: ciName, description, terminateOtherVersions, isInfrastructureAgent, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        customerEnvironmentServicesMock.Verify(x => x.CreateCustomerEnvironmentForCustomerInfrastructure(It.IsAny<CustomerEnvironment>(), ciName, false), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(customerEnvironment), Times.Never);
        
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(deploymentPackage.Name, 0), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicensesByUniqueName(licenseNames), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateDeploymentPackage(It.IsAny<CustomerEnvironment>(), deploymentPackage,
            It.Is<long[]>(
                x => x.Count() == 1 && x[0] == cPSoftwareLicense.Id
            )), Times.Once);
    }

    [Fact]
    public async void Run_CustomerEnvironmentExists_DeploymentPackageIsUpdated()
    {
        // Arrange
        using var mock = AutoMock.GetLoose();

        var customerEnvironment = new CustomerEnvironment() { Name = name };
        var deploymentPackage = new DeploymentPackage() { Name = deploymentPackageName };
        var cPSoftwareLicense = new CPSoftwareLicense() { Name = licenseName, Id = 1234 };
        string[] licenseNames = [licenseName];
        List<CPSoftwareLicense> licenses = [cPSoftwareLicense];

        var newEnvironmentUtilitiesMock = mock.Mock<INewEnvironmentUtilities>();

        var customerPortalClientMock = mock.Mock<ICustomerPortalClient>();
        customerPortalClientMock
            .Setup(x => x.GetObjectByName<DeploymentPackage>(deploymentPackage.Name, 0))
            .Returns(Task.FromResult(deploymentPackage));

        var customerEnvironmentServicesMock = mock.Mock<ICustomerEnvironmentServices>();
        customerEnvironmentServicesMock
            .Setup(x => x.GetCustomerEnvironment(It.IsAny<ISession>(), customerEnvironment.Name))
            .Returns(Task.FromResult(customerEnvironment));
        customerEnvironmentServicesMock
            .Setup(x => x.CreateEnvironment(customerPortalClientMock.Object, customerEnvironment))
            .Returns(Task.FromResult(customerEnvironment));
        customerEnvironmentServicesMock
            .Setup(x => x.UpdateEnvironment(customerEnvironment))
            .Returns(Task.FromResult(customerEnvironment));


        var licenseServiceMock = mock.Mock<ILicenseServices>();
        licenseServiceMock
            .Setup(x => x.GetLicensesByUniqueName(licenseNames))
            .Returns(Task.FromResult(licenses.AsEnumerable()));

        // Act
        var newEnvironmentHandler = mock.Create<NewEnvironmentHandler>();
        await newEnvironmentHandler.Run(name, parameters, environmentType, siteName, licenseNames, deploymentPackageName, target, outputDir, replaceTokens, interactive,
            customerInfrastructureName: "infrastructure Name", description, terminateOtherVersions, isInfrastructureAgent, minutesTimeoutMainTask, minutesTimeoutToGetSomeMBMsg,
            terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes);

        // Assert
        newEnvironmentUtilitiesMock.Verify(x => x.CheckEnvironmentConnection(It.IsAny<CustomerEnvironment>()), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.GetCustomerEnvironment(It.IsAny<ISession>(), customerEnvironment.Name), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.CreateEnvironment(customerPortalClientMock.Object, customerEnvironment), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateEnvironment(customerEnvironment), Times.Once);
        
        customerPortalClientMock.Verify(x => x.GetObjectByName<DeploymentPackage>(deploymentPackage.Name, 0), Times.Once);
        licenseServiceMock.Verify(x => x.GetLicensesByUniqueName(licenseNames), Times.Once);
        customerEnvironmentServicesMock.Verify(x => x.UpdateDeploymentPackage(customerEnvironment, deploymentPackage,
            It.Is<long[]>(
                x => x.Count() == 1 && x[0] == cPSoftwareLicense.Id
            )), Times.Once);
    }
}
