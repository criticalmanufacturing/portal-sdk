using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    /// <summary>
    /// Adds the <c>--replace-tokens</c> option.
    /// </summary>
    internal class ReplaceTokensExtension : IOptionExtension
    {
        public Option<string[]> ReplaceTokens { get; } = new("--replace-tokens")
        {
            Description = Resources.ReplaceTokensHelp,
            HelpName = "MyToken=value MyToken2=value2",
            AllowMultipleArgumentsPerToken = true
        };

        public void Use(Command command)
        {
            command.Options.Add(ReplaceTokens);
        }
    }
}
