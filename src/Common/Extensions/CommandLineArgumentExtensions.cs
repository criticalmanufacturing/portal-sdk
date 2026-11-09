namespace Cmf.CustomerPortal.Sdk.Common.Extensions;

/// <summary>
/// Quotes values, such as Customer Environment or App names, for the commands that log messages suggest, so a
/// value with spaces or quotes still pastes as a single argument.
/// </summary>
internal static class CommandLineArgumentExtensions
{
    /// <summary>
    /// Quotes <paramref name="value"/> as a <c>cmf-portal</c> argument: in double quotes, with embedded double quotes
    /// escaped as <c>\"</c>.
    /// </summary>
    /// <param name="value">The value to quote. <c>null</c> is quoted as an empty value.</param>
    /// <returns>The quoted value.</returns>
    public static string ToCliArgument(this string? value) => $"\"{value?.Replace("\"", "\\\"")}\"";

    /// <summary>
    /// Quotes <paramref name="value"/> as a PowerShell argument: in single quotes, with embedded single quotes doubled.
    /// </summary>
    /// <param name="value">The value to quote. <c>null</c> is quoted as an empty value.</param>
    /// <returns>The quoted value.</returns>
    public static string ToPowerShellArgument(this string? value) => $"'{value?.Replace("'", "''")}'";
}
