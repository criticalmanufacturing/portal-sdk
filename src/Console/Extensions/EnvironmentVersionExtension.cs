using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions;

/// <summary>
/// Adds the <c>--version</c> option, which selects a Customer Environment version by its version number.
/// </summary>
internal class EnvironmentVersionExtension : IOptionExtension
{
    public Option<int?> Version { get; } = new("--version", "-ver")
    {
        Description = Resources.EnvironmentVersionHelp,
        HelpName = "version"
    };

    public EnvironmentVersionExtension()
    {
        Version.Validators.Add(ValidatePositive);
    }

    public void Use(Command command)
    {
        command.Options.Add(Version);
    }

    private void ValidatePositive(OptionResult result)
    {
        // versions 0 and below are the Customer Environment's definition rows, not versions.
        // Validators also run when the value isn't a number, which is already reported as a parse error.
        if (result.Tokens.Count == 1 && int.TryParse(result.Tokens[0].Value, out int version) && version < 1)
        {
            result.AddError(Resources.EnvironmentVersionInvalidMessage);
        }
    }
}
