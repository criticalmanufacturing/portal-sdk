using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Extensions
{
    internal class ReplaceTokensExtension : IOptionExtension
    {
        public void Use(Command command)
        {
            var replaceTokensOption = new Option<string[]>(new[] { "--replace-tokens" }, Resources.REPLACETOKENS_HELP)
            {
                AllowMultipleArgumentsPerToken = true
            };
            replaceTokensOption.AddSuggestions(new string[] { "MyToken=value MyToken2=value2" });
            command.Add(replaceTokensOption);
        }
    }
}
