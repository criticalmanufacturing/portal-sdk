﻿using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Base
{
    abstract class ReplaceTokensBaseCommand : BaseCommand
    {
        public ReplaceTokensBaseCommand(string name, string description) : base(name, description)
        {
            var replaceTokensOption = new Option<string[]>(new[] { "--replace-tokens" }, Resources.REPLACETOKENS_HELP)
            {
                AllowMultipleArgumentsPerToken = true
            };
            replaceTokensOption.AddSuggestions(new string[] { "MyToken=value MyToken2=value2" });
            Add(replaceTokensOption);
        }
    }
}
