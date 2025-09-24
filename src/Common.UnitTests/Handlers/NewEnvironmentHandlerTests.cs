using Autofac.Extras.Moq;
using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.BusinessObjects;
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
    
    [Fact]
    public async Task Run_ShouldLogTerminationLogsAndThrow_WhenTerminationFails()
    {
        // Arrange
        var mockClient = new Mock<ICustomerPortalClient>();
        var mockUtils = new Mock<INewEnvironmentUtilities>();
        var mockEnvDeploymentHandler = new Mock<IEnvironmentDeploymentHandler>();
        var mockEnvServices = new Mock<ICustomerEnvironmentServices>();
        var mockLicenseServices = new Mock<ILicenseServices>();
        var mockSession = new Mock<ISession>();

        // Capture session logs
        var loggedExceptionObjects = new List<Exception>();
        var loggedStrings = new List<string>();

        mockSession.Setup(s => s.LogError(It.IsAny<Exception>()))
            .Callback<Exception>(ex => loggedExceptionObjects.Add(ex));
        mockSession.Setup(s => s.LogError(It.IsAny<string>()))
            .Callback<string>(msg => loggedStrings.Add(msg));
        mockSession.Setup(s => s.LogInformation(It.IsAny<string>()))
            .Callback<string>(_ => { /* ignore info logs */ });

        // Existing environment returned by GetCustomerEnvironment -> triggers "environment != null" branch
        var existingEnvironment = new CustomerEnvironment { Id = 1, Name = "env-existing" };
        mockEnvServices.Setup(s => s.GetCustomerEnvironment(mockSession.Object, It.IsAny<string>()))
            .ReturnsAsync(existingEnvironment);

        // Utilities and services behavior
        mockUtils.Setup(u => u.GetDeploymentTargetValue(It.IsAny<DeploymentTarget>()))
            .Returns("SomeTarget");
        mockUtils.Setup(u => u.CheckEnvironmentConnection(It.IsAny<CustomerEnvironment>()))
            .Returns(Task.FromResult(new List<CustomerEnvironment>()));

        // Simulate created/updated environment returned after CreateEnvironment/UpdateEnvironment
        mockEnvServices.Setup(s => s.CreateEnvironment(mockClient.Object, It.IsAny<CustomerEnvironment>()))
            .ReturnsAsync(existingEnvironment);
        mockEnvServices.Setup(s => s.UpdateEnvironment(It.IsAny<CustomerEnvironment>()))
            .ReturnsAsync(existingEnvironment);

        // Prepare other versions to terminate (non-empty list so termination logic runs)
        var otherEnv1 = new CustomerEnvironment { Id = 10, Name = "env-old-1" };
        var otherEnv2 = new CustomerEnvironment { Id = 20, Name = "env-old-2" };
        var toTerminate = new CustomerEnvironmentCollection { otherEnv1, otherEnv2 };
        mockUtils.Setup(u => u.GetOtherVersionToTerminate(It.IsAny<CustomerEnvironment>()))
            .ReturnsAsync(toTerminate);

        // TerminateObjects should be called (we just return completed task)
        mockClient.Setup(c => c.TerminateObjects<List<CustomerEnvironment>, CustomerEnvironment>(
                It.IsAny<List<CustomerEnvironment>>(),
                It.IsAny<OperationAttributeCollection>(),
                /* cancellationToken: */ default
            ))
            .ReturnsAsync([]);

        // Simulate that WaitForEnvironmentsToBeTerminated returns one failed id (20)
        mockEnvDeploymentHandler.Setup(h => h.WaitForEnvironmentsToBeTerminated(It.IsAny<CustomerEnvironmentCollection>()))
            .ReturnsAsync([20]);

        // Provide termination logs for failed id 20
        mockClient.Setup(c => c.GetCustomerEnvironmentTerminationLogs(20))
            .ReturnsAsync("termination logs for 20");

        // Instantiate handler (isInfrastructureAgent true to avoid deployment package updates)
        var handler = new NewEnvironmentHandler(
            mockClient.Object,
            mockSession.Object,
            mockUtils.Object,
            mockEnvDeploymentHandler.Object,
            mockEnvServices.Object,
            mockLicenseServices.Object
        );

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => handler.Run(
            name: existingEnvironment.Name,
            parameters: null,
            environmentType: EnvironmentType.Production,
            siteName: null,
            licensesNames: [],
            deploymentPackageName: null,
            target: DeploymentTarget.KubernetesOnPremisesTarget,
            outputDir: null,
            replaceTokens: [],
            interactive: false,
            customerInfrastructureName: null,
            description: null,
            terminateOtherVersions: true,
            isInfrastructureAgent: true,
            minutesTimeoutMainTask: null,
            minutesTimeoutToGetSomeMBMsg: null,
            terminateOtherVersionsRemove: false,
            terminateOtherVersionsRemoveVolumes: false
        ));

        // Exception message should mention stopping deploy process and include failed id 20
        Assert.Contains("Stopping deploy process because termination of other environment versions failed", ex.Message);
        Assert.Contains("20", ex.Message);

        // Ensure an exception object was logged via Session.LogError(Exception)
        Assert.NotEmpty(loggedExceptionObjects);
        Assert.Contains(loggedExceptionObjects, e => e.Message.Contains("Stopping deploy process"));

        // Ensure termination logs were requested for the failed id and the logs were logged as error strings
        mockClient.Verify(c => c.GetCustomerEnvironmentTerminationLogs(20), Times.Once);

        Assert.NotEmpty(loggedStrings);
        Assert.Contains(loggedStrings, s => s.Contains("Customer Environment 20") && s.Contains("termination logs for 20"));
    }
}
