using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.Foundation.Common.Base;
using Cmf.LightBusinessObjects.Infrastructure.Errors;
using Moq;

namespace Common.UnitTests.Handlers;

public class GetDeploymentPackageHandlerTests
{
    private const string PackageName = "@criticalmanufacturing\\mes:12.0.0";

    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerPortalClient> _customerPortalClientMock = new();

    private readonly GetDeploymentPackageHandler _handler;

    public GetDeploymentPackageHandlerTests()
    {
        _handler = new GetDeploymentPackageHandler(_sessionMock.Object, _customerPortalClientMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _handler.Run(name!));
        _customerPortalClientMock.Verify(c => c.GetObjectByName<DeploymentPackage>(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task Run_ReturnsTheDetailsOfTheDeploymentPackage()
    {
        // Arrange
        var createdOn = new DateTime(2026, 10, 13, 16, 33, 23);
        _customerPortalClientMock.Setup(c => c.GetObjectByName<DeploymentPackage>(PackageName, 0))
            .ReturnsAsync(new DeploymentPackage
            {
                Id = 7,
                Name = PackageName,
                DisplayName = "MES",
                Description = " ",
                PackageVersion = "12.0.0",
                UniversalState = UniversalState.Effective,
                IsRootPackage = true,
                IsInstallable = false,
                PreRelease = null,
                ManifestVersion = "1.0.0",
                LicenseVersion = "",
                DocumentationURL = "https://help.example.com",
                CreatedBy = "user",
                CreatedOn = createdOn,
                ModifiedBy = "other",
                ModifiedOn = createdOn.AddHours(1)
            });

        // Act
        DeploymentPackageDetails details = await _handler.Run(PackageName);

        // Assert
        Assert.Equal(
            new DeploymentPackageDetails(7, PackageName, "MES", null, "12.0.0", "Effective", true, false, null, "1.0.0", null,
                "https://help.example.com", "user", createdOn, "other", createdOn.AddHours(1)),
            details);
    }

    [Fact]
    public async Task Run_WhenDeploymentPackageDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _customerPortalClientMock.Setup(c => c.GetObjectByName<DeploymentPackage>(PackageName, 0))
            .ThrowsAsync(new CmfFaultException("action", new CmfFaultCode("Db20001", null), null, "not found"));

        // Act
        var exception = await Assert.ThrowsAsync<NotFoundException>(() => _handler.Run(PackageName));

        // Assert
        Assert.Equal($"Deployment Package '{PackageName}' does not exist", exception.Message);
    }

    [Fact]
    public async Task Run_WhenThePortalFailsForAnotherReason_RethrowsTheFault()
    {
        // Arrange
        _customerPortalClientMock.Setup(c => c.GetObjectByName<DeploymentPackage>(PackageName, 0))
            .ThrowsAsync(new CmfFaultException("action", new CmfFaultCode("Other", null), null, "failure"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<CmfFaultException>(() => _handler.Run(PackageName));
        Assert.IsNotType<NotFoundException>(exception);
    }
}
