using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Extensions;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System;
using System.IO.Abstractions;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Gets the installation and termination logs of a Customer Environment version, and writes them to a file.
/// </summary>
public class GetEnvironmentLogsHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient,
    ICustomerEnvironmentServices customerEnvironmentServices,
    IFileSystem fileSystem) : AbstractHandler(session, true)
{
    /// <summary>
    /// Key of the installation logs in the result.
    /// </summary>
    public const string InstallationKey = "installation";

    /// <summary>
    /// Key of the termination logs in the result.
    /// </summary>
    public const string TerminationKey = "termination";

    /// <summary>
    /// Gets the logs of a Customer Environment version.
    /// </summary>
    /// <param name="name">Name of the Customer Environment.</param>
    /// <param name="version">Version number of the version, or <c>null</c> for the current version.</param>
    /// <param name="logType">The logs to get.</param>
    /// <returns>
    /// A JSON object with the requested logs keyed by <see cref="InstallationKey"/> and <see cref="TerminationKey"/>.
    /// Logs that are JSON are kept as JSON, any other logs as a string, and missing logs are <c>null</c>.
    /// </returns>
    /// <exception cref="NotFoundException">The Customer Environment, or the requested version, does not exist.</exception>
    public async Task<JsonObject> Run(string name, int? version, EnvironmentLogType logType)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        await EnsureLogin();

        CustomerEnvironment environment = await customerEnvironmentServices.GetCustomerEnvironmentVersion(name, version);

        var logs = new JsonObject();
        if (logType is EnvironmentLogType.All or EnvironmentLogType.Installation)
        {
            Session.LogDebug($"Getting the installation logs of customer environment {name} {environment.DescribeVersion()}");
            logs[InstallationKey] = ParseLogs(await customerPortalClient.GetCustomerEnvironmentDeploymentLogs(environment.Id));
        }

        if (logType is EnvironmentLogType.All or EnvironmentLogType.Termination)
        {
            Session.LogDebug($"Getting the termination logs of customer environment {name} {environment.DescribeVersion()}");
            logs[TerminationKey] = ParseLogs(await customerPortalClient.GetCustomerEnvironmentTerminationLogs(environment.Id));
        }

        return logs;
    }

    /// <summary>
    /// Writes logs got with <see cref="Run"/>, already formatted, to a file.
    /// </summary>
    /// <param name="contents">The formatted logs.</param>
    /// <param name="output">
    /// Path of the file to write, or of a directory (an existing one, or a path ending in a separator) where the file is
    /// named <c>&lt;name&gt;-logs&lt;extension&gt;</c>, or <c>&lt;name&gt;-v&lt;version&gt;-logs&lt;extension&gt;</c> for
    /// a specific version. Missing directories are created and an existing file is overwritten.
    /// </param>
    /// <param name="name">Name of the Customer Environment.</param>
    /// <param name="version">Version number of the version, or <c>null</c> for the current version.</param>
    /// <param name="extension">Extension of the file named after the Customer Environment, e.g. <c>.json</c>.</param>
    /// <returns>The full path of the written file.</returns>
    public string WriteLogs(string contents, string output, string name, int? version, string extension)
    {
        ArgumentNullException.ThrowIfNull(contents);
        ArgumentException.ThrowIfNullOrWhiteSpace(output);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);

        string path = fileSystem.Path.GetFullPath(output);
        bool isDirectory = fileSystem.Directory.Exists(path)
            || output.EndsWith(fileSystem.Path.DirectorySeparatorChar)
            || output.EndsWith(fileSystem.Path.AltDirectorySeparatorChar);
        if (isDirectory)
        {
            path = fileSystem.Path.Combine(path, version is null ? $"{name}-logs{extension}" : $"{name}-v{version}-logs{extension}");
        }

        string? directory = fileSystem.Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            fileSystem.Directory.CreateDirectory(directory);
        }

        fileSystem.File.WriteAllText(path, contents);
        Session.LogInformation(string.Format(Resources.LogsWrittenMessage, path));

        return path;
    }

    private static JsonNode? ParseLogs(string? logs)
    {
        if (string.IsNullOrWhiteSpace(logs))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(logs);
        }
        catch (JsonException)
        {
            return JsonValue.Create(logs);
        }
    }
}
