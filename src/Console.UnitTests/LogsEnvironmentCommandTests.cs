using Cmf.CustomerPortal.Sdk.Console.Commands.Logs;
using System.Text.Json.Nodes;

namespace Console.UnitTests;

/// <summary>
/// The table written by <c>logs env</c>.
/// </summary>
public class LogsEnvironmentCommandTests
{
    private static string[] WriteTable(string logs)
    {
        using var output = new StringWriter();
        LogsEnvironmentCommand.WriteTable(output, JsonNode.Parse(logs)!.AsObject());
        return output.ToString().Split(Environment.NewLine)[..^1];
    }

    [Fact]
    public void WriteTable_WritesASectionPerLogTypeWithColumnsAlignedAcrossSections()
    {
        // Act
        string[] lines = WriteTable("""
            {
              "installation": [
                { "StepId": "Pre", "Data": "Agent connection ok", "DeploymentStatus": 1, "LogLevel": null },
                { "StepId": "Connect To Cluster", "Data": "Connected", "DeploymentStatus": 1, "LogLevel": 2 }
              ],
              "termination": [
                { "StepId": "Delete", "Data": "Could not delete the Job", "DeploymentStatus": 6, "LogLevel": 3 }
              ]
            }
            """);

        // Assert
        Assert.Equal(
            [
                "Installation logs:",
                "LogLevel | StepId             | Data",
                "INFO     | Pre                | Agent connection ok",
                "INFO     | Connect To Cluster | Connected",
                "",
                "Termination logs:",
                "LogLevel | StepId             | Data",
                "WARNING  | Delete             | Could not delete the Job"
            ],
            lines);
    }

    [Theory]
    [InlineData("0", "TRACE")]
    [InlineData("1", "DEBUG")]
    [InlineData("2", "INFO")]
    [InlineData("3", "WARNING")]
    [InlineData("4", "ERROR")]
    [InlineData("5", "CRITICAL")]
    [InlineData("6", "6")]
    [InlineData("null", "INFO")]
    [InlineData("\"\"", "INFO")]
    [InlineData("\"Warning\"", "WARNING")]
    public void WriteTable_NamesTheLogLevel(string logLevel, string expectedName)
    {
        // Act
        string[] lines = WriteTable($$"""{ "installation": [ { "StepId": "Pre", "Data": "message", "LogLevel": {{logLevel}} } ] }""");

        // Assert
        Assert.StartsWith(expectedName + " ", lines[2]);
    }

    [Fact]
    public void WriteTable_WithoutALogLevelField_UsesInfo()
    {
        // Act
        string[] lines = WriteTable("""{ "installation": [ { "StepId": "Pre", "Data": "message" } ] }""");

        // Assert
        Assert.Equal("INFO     | Pre    | message", lines[2]);
    }

    [Fact]
    public void WriteTable_KeepsTheLinesOfAMultiLineEntryInTheDataColumn()
    {
        // Act
        string[] lines = WriteTable("""
            { "installation": [ { "StepId": "Post", "Data": "Failed to deploy.\r\n Object reference not set.\n", "LogLevel": 4 } ] }
            """);

        // Assert
        Assert.Equal(
            [
                "Installation logs:",
                "LogLevel | StepId | Data",
                "ERROR    | Post   | Failed to deploy.",
                "         |        |  Object reference not set."
            ],
            lines);
    }

    [Fact]
    public void WriteTable_ShowsMissingAndEmptyLogsAndFieldsAsADash()
    {
        // Act
        string[] lines = WriteTable("""{ "installation": [ { "StepId": null, "Data": "" } ], "termination": null }""");

        // Assert
        Assert.Equal(
            [
                "Installation logs:",
                "LogLevel | StepId | Data",
                "INFO     | -      | -",
                "",
                "Termination logs: -"
            ],
            lines);
    }

    [Fact]
    public void WriteTable_WithNoEntries_ShowsADash()
    {
        // Act
        string[] lines = WriteTable("""{ "termination": [] }""");

        // Assert
        Assert.Equal(["Termination logs: -"], lines);
    }

    [Fact]
    public void WriteTable_ShowsLogsThatAreNotAListOfEntriesAsTheyAre()
    {
        // Act
        string[] lines = WriteTable("""{ "installation": "plain text log" }""");

        // Assert
        Assert.Equal(["Installation logs:", "plain text log"], lines);
    }
}
