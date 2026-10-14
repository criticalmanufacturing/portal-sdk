using Cmf.CustomerPortal.BusinessObjects;
using Cmf.CustomerPortal.Sdk.Common.Services;
using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Common.Handlers;

/// <summary>
/// Gets the installation and termination logs of a Customer Environment version.
/// </summary>
public class GetEnvironmentLogsHandler(
    ISession session,
    ICustomerPortalClient customerPortalClient,
    ICustomerEnvironmentServices customerEnvironmentServices) : AbstractHandler(session, true)
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
            Session.LogDebug($"Getting the installation logs of customer environment {name} (Id {environment.Id})");
            logs[InstallationKey] = ParseLogs(await customerPortalClient.GetCustomerEnvironmentDeploymentLogs(environment.Id));
        }

        if (logType is EnvironmentLogType.All or EnvironmentLogType.Termination)
        {
            Session.LogDebug($"Getting the termination logs of customer environment {name} (Id {environment.Id})");
            logs[TerminationKey] = ParseLogs(await customerPortalClient.GetCustomerEnvironmentTerminationLogs(environment.Id));
        }

        return logs;
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
