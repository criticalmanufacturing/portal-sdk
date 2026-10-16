using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Get;

namespace Console.UnitTests;

/// <summary>
/// The table written by <c>get deploymentpackage</c>.
/// </summary>
public class GetDeploymentPackageCommandTests
{
    [Fact]
    public void WriteTable_WritesEveryFieldAlignedWithADashForEmptyOnes()
    {
        // Arrange
        var createdOn = new DateTime(2026, 10, 13, 16, 33, 23);
        var details = new DeploymentPackageDetails(7, "@criticalmanufacturing\\mes:12.0.0", "MES", null, "12.0.0", "Effective",
            true, false, null, "1.0.0", null, null, "user", createdOn, null, default);
        using var output = new StringWriter();

        // Act
        GetDeploymentPackageCommand.WriteTable(output, details);

        // Assert
        Assert.Equal(
            [
                "Id:                7",
                "Name:              @criticalmanufacturing\\mes:12.0.0",
                "Display Name:      MES",
                "Description:       -",
                "Package Version:   12.0.0",
                "Universal State:   Effective",
                "Is Root Package:   True",
                "Is Installable:    False",
                "Pre-Release:       -",
                "Manifest Version:  1.0.0",
                "License Version:   -",
                "Documentation URL: -",
                "Created By:        user",
                "Created On:        2026-10-13 16:33:23",
                "Modified By:       -",
                "Modified On:       -"
            ],
            output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries));
    }

    [Fact]
    public void WriteTable_HasOneRowPerField()
    {
        // Arrange
        var details = new DeploymentPackageDetails(7, "package", "name", "desc", "1.0.0", "Effective", true, true, true, "1.0.0",
            "1", "url", "user", DateTime.Now, "user", DateTime.Now);
        using var output = new StringWriter();

        // Act
        GetDeploymentPackageCommand.WriteTable(output, details);

        // Assert
        int fields = typeof(DeploymentPackageDetails).GetProperties().Length;
        Assert.Equal(fields, output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Length);
    }
}
