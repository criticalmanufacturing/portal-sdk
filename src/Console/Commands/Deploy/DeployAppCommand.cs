using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Deploy
{
    /// <summary>
    /// <c>deploy app &lt;name&gt;</c>: installs an App in a Customer Environment.
    /// </summary>
    class DeployAppCommand : DeployAppCommandBase
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.AppNameHelp };

        public DeployAppCommand() : base("app", Resources.NounDeployAppHelp)
        {
            Aliases.Add("application");
            Arguments.Add(nameArgument);
        }

        protected override string GetAppName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
