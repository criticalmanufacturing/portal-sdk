namespace Cmf.CustomerPortal.Sdk.Common;

/// <summary>
/// Help texts shared by the Console and PowerShell front ends. Split by area into partial class files.
/// </summary>
public static partial class Resources
{
    public const string VerboseHelp = "Show detailed logging";

    public const string LoginPatHelp = "Personal Access Token used to access the Customer Portal";

    public const string ReplaceTokensHelp = "Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values. E.g. MyToken=value MyToken2=value2.";

    public const string CustomerEnvironmentNameHelp = "Name of the Customer Environment to be used.";
}
