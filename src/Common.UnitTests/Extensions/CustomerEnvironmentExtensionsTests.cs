using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;

namespace Common.UnitTests.Extensions;

public class CustomerEnvironmentExtensionsTests
{
    [Fact]
    public void DescribeVersion_ReturnsTheVersionNumberAndId()
    {
        // Arrange
        var environment = new CustomerEnvironment { Name = "env", Id = 1234, Version = 3 };

        // Act
        string description = environment.DescribeVersion();

        // Assert
        Assert.Equal("version 3 (Id 1234)", description);
    }
}
