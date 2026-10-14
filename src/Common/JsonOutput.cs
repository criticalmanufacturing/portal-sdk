using System.Text.Encodings.Web;
using System.Text.Json;

namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Settings for the JSON shown to the user, either on the standard output or in a file.
/// </summary>
public static class JsonOutput
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
