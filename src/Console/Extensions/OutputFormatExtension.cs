using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions;

/// <summary>
/// Adds the <c>--format</c> option, which selects a readable table or JSON for the command's output.
/// </summary>
internal class OutputFormatExtension : IOptionExtension
{
    public Option<OutputFormat> Format { get; } = new("--format", "-f")
    {
        Description = Resources.OutputFormatHelp,
        DefaultValueFactory = _ => OutputFormat.Table
    };

    public void Use(Command command)
    {
        command.Options.Add(Format);
    }
}
