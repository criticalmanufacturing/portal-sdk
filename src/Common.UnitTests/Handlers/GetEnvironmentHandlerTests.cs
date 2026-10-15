using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Common.Services;
using Cmf.Foundation.Common.Base;
using Moq;

namespace Common.UnitTests.Handlers;

public class GetEnvironmentHandlerTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly Mock<ICustomerEnvironmentServices> _customerEnvironmentServicesMock = new();

    private readonly GetEnvironmentHandler _handler;

    public GetEnvironmentHandlerTests()
    {
        _handler = new GetEnvironmentHandler(_sessionMock.Object, _customerEnvironmentServicesMock.Object);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task Run_WhenNameIsNullOrWhitespace_ThrowsArgumentException(string? name)
    {
        // Act & Assert
        await Assert.ThrowsAnyAsync<ArgumentException>(() => _handler.Run(name!, null));
        _customerEnvironmentServicesMock.Verify(s => s.GetCustomerEnvironmentVersion(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<int>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(3)]
    public async Task Run_ReturnsTheDetailsOfTheRequestedVersion(int? version)
    {
        // Arrange
        var createdOn = new DateTime(2026, 10, 13, 16, 33, 23);
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironmentVersion("env", version, 1))
            .ReturnsAsync(new CustomerEnvironment
            {
                Id = 7,
                Name = "env",
                Version = 3,
                Description = " ",
                EnvironmentType = "Development",
                Status = DeploymentStatus.DeploymentSucceeded,
                UniversalState = UniversalState.Effective,
                Site = new ProductSite { Name = "site" },
                CustomerInfrastructure = new CustomerInfrastructure { Name = "infra" },
                DeploymentPackage = new DeploymentPackage { Name = "@criticalmanufacturing\\mes:12.0.0" },
                DeploymentTarget = "OpenShiftRemoteTarget",
                CreatedBy = "user",
                CreatedOn = createdOn,
                ModifiedBy = "other",
                ModifiedOn = createdOn.AddHours(1)
            });

        // Act
        CustomerEnvironmentDetails details = await _handler.Run("env", version);

        // Assert
        Assert.Equal(
            new CustomerEnvironmentDetails(7, "env", 3, null, "Development", "DeploymentSucceeded", "Effective", "site", "infra",
                "@criticalmanufacturing\\mes:12.0.0", "OpenShiftRemoteTarget", "user", createdOn, "other", createdOn.AddHours(1)),
            details);
    }

    [Fact]
    public async Task Run_WhenVersionDoesNotExist_ThrowsNotFoundException()
    {
        // Arrange
        _customerEnvironmentServicesMock.Setup(s => s.GetCustomerEnvironmentVersion("env", 3, 1))
            .ThrowsAsync(new NotFoundException("not found"));

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(() => _handler.Run("env", 3));
    }
}
