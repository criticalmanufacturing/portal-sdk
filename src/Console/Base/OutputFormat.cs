namespace Cmf.CustomerPortal.Sdk.Console.Base;

/// <summary>
/// Format of the output of the commands that show a resource.
/// </summary>
enum OutputFormat
{
    /// <summary>
    /// Readable text: <c>Label: value</c> lines or aligned columns, with <see cref="TableOutput.EmptyValue"/> for empty values.
    /// </summary>
    Table,

    /// <summary>
    /// Indented JSON, for scripts.
    /// </summary>
    Json
}
