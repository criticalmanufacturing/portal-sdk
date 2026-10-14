using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Get;

namespace Console.UnitTests;

/// <summary>
/// The table written by <c>get env</c>.
/// </summary>
public class GetEnvironmentCommandTests
{
    [Fact]
    public void WriteTable_AlignsValuesAndSkipsEmptyOnes()
    {
        // Arrange
        var createdOn = new DateTime(2026, 10, 13, 16, 33, 23);
        var details = new CustomerEnvironmentDetails(7, "env", 3, null, "Development", "NotDeployed", "Created", null, null,
            "@criticalmanufacturing\\mes:12.0.0", "OpenShiftRemoteTarget", "user", createdOn, null, default);
        using var output = new StringWriter();

        // Act
        GetEnvironmentCommand.WriteTable(output, details);

        // Assert
        Assert.Equal(
            [
                "Name:                    env",
                "Id:                      7",
                "Version:                 3",
                "Type:                    Development",
                "Status:                  NotDeployed",
                "Universal State:         Created",
                "Deployment Package:      @criticalmanufacturing\\mes:12.0.0",
                "Deployment Target:       OpenShiftRemoteTarget",
                "Created:                 2026-10-13 16:33:23 by user"
            ],
            output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }
}
