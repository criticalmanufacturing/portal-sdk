namespace Cmf.CustomerPortal.Sdk.Console.Base;

/// <summary>
/// Format of the output of the commands that show a resource.
/// </summary>
enum OutputFormat
{
    /// <summary>
    /// Readable <c>Label: value</c> lines.
    /// </summary>
    Table,

    /// <summary>
    /// Indented JSON, for scripts.
    /// </summary>
    Json
}
