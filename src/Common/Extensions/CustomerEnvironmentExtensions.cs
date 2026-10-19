using Cmf.CustomerPortal.BusinessObjects;

namespace Cmf.CustomerPortal.Sdk.Common.Extensions;

/// <summary>
/// Extensions to describe a <see cref="CustomerEnvironment"/> in log messages.
/// </summary>
public static class CustomerEnvironmentExtensions
{
    /// <summary>
    /// Describes which version of a Customer Environment this is, e.g. <c>version 3 (Id 1234)</c>.
    /// </summary>
    /// <param name="environment">The Customer Environment version.</param>
    /// <returns>The version number followed by the Id of the version.</returns>
    public static string DescribeVersion(this CustomerEnvironment environment)
        => $"version {environment.Version} (Id {environment.Id})";
}
