namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts and messages for getting the details and the logs of Customer Environments.
/// </summary>
public static partial class Resources
{
    public const string EnvironmentVersionHelp = "Version number of the Customer Environment version to use. The current version is used when omitted";
    public const string OutputFormatHelp = "Format of the output: a readable table, or JSON for scripts";
    public const string LogTypeHelp = "Type of logs to get: the installation logs, the termination logs, or both";
    public const string LogsOutputHelp = "File to write the logs to, or a directory to write them to as <name>-logs.json (<name>-v<version>-logs.json with --version). The logs are written to the standard output when omitted";

    public const string EnvironmentNotFoundMessage = "Customer Environment '{0}' does not exist";
    public const string EnvironmentVersionNotFoundMessage = "Customer Environment '{0}' has no version {1}";
    public const string EnvironmentVersionInvalidMessage = "The version must be a positive number";
    public const string LogsWrittenMessage = "Logs written to {0}";

    // messages printed when a preview command runs
    public const string GetEnvironmentPreview = "[Preview] 'get env' is a preview: its options and output may change in a future version.";
    public const string LogsEnvironmentPreview = "[Preview] 'logs env' is a preview: its options and output may change in a future version.";
}
