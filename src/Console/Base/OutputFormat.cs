using System.Text.Encodings.Web;
using System.Text.Json;

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

/// <summary>
/// Settings for the JSON written to the standard output.
/// </summary>
static class JsonOutput
{
    /// <summary>
    /// Indented, camelCase JSON. Characters are not escaped beyond what JSON requires, so logs stay readable.
    /// </summary>
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
}
