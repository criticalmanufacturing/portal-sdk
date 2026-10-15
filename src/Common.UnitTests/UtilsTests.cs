using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common;
using Moq;

namespace Common.UnitTests;

public class UtilsTests
{
    private readonly Mock<ISession> _sessionMock = new();
    private readonly CustomerEnvironment _environment = new() { Name = "env", Id = 1234, Version = 3 };

    [Theory]
    [InlineData(true, "https")]
    [InlineData(false, "http")]
    public void LogCustomerEnvironmentUrl_WhenTheHostIsConfigured_LogsTheInstallationPageOfTheVersion(bool useSsl, string scheme)
    {
        // Arrange
        _sessionMock.Setup(s => s.Configuration).Returns(PortalConfiguration.Create(useSsl));

        // Act
        Utils.LogCustomerEnvironmentUrl(_sessionMock.Object, _environment);

        // Assert
        _sessionMock.Verify(s => s.LogInformation($"{scheme}://portal.example.com/Entity/CustomerEnvironment/1234/View/Installation"), Times.Once);
    }

    [Fact]
    public void LogCustomerEnvironmentUrl_WhenTheHostIsNotConfigured_LogsNothing()
    {
        // Act
        Utils.LogCustomerEnvironmentUrl(_sessionMock.Object, _environment);

        // Assert
        _sessionMock.Verify(s => s.LogInformation(It.IsAny<string>()), Times.Never);
    }
}
