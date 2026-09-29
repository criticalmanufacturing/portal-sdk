using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>install-app</c> command. Superseded by <c>deploy app</c>.
    /// </summary>
    class InstallAppCommand : DeployAppCommandBase
    {
        private readonly Option<string> nameOption = new("--name", "-n") { Description = Resources.AppNameHelp, Required = true };

        public InstallAppCommand() : base("install-app", "Install an App in a previously deployed Convergence environment.")
        {
            Options.Add(nameOption);
        }

        protected override string GetAppName(ParseResult parseResult) => parseResult.GetValue(nameOption);
    }
}
