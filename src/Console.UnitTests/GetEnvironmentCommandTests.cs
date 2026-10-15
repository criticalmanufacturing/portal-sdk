using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Get;

namespace Console.UnitTests;

/// <summary>
/// The table written by <c>get env</c>.
/// </summary>
public class GetEnvironmentCommandTests
{
    [Fact]
    public void WriteTable_WritesEveryFieldAlignedWithADashForEmptyOnes()
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
                "Id:                      7",
                "Name:                    env",
                "Version:                 3",
                "Description:             -",
                "Environment Type:        Development",
                "Status:                  NotDeployed",
                "Universal State:         Created",
                "Site:                    -",
                "Customer Infrastructure: -",
                "Deployment Package:      @criticalmanufacturing\\mes:12.0.0",
                "Deployment Target:       OpenShiftRemoteTarget",
                "Created By:              user",
                "Created On:              2026-10-13 16:33:23",
                "Modified By:             -",
                "Modified On:             -"
            ],
            output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void WriteTable_HasOneRowPerJsonField()
    {
        // Arrange
        var details = new CustomerEnvironmentDetails(7, "env", 3, "desc", "Development", "NotDeployed", "Created", "site", "infra",
            "package", "target", "user", DateTime.Now, "user", DateTime.Now);
        using var output = new StringWriter();

        // Act
        GetEnvironmentCommand.WriteTable(output, details);

        // Assert
        int jsonFields = typeof(CustomerEnvironmentDetails).GetProperties().Length;
        Assert.Equal(jsonFields, output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
