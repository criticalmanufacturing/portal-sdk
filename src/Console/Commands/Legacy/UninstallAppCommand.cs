using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>uninstall-app</c> command. Superseded by <c>undeploy app</c>.
    /// </summary>
    class UninstallAppCommand : UndeployAppCommandBase
    {
        private readonly Option<string> nameOption = new("--name", "-n") { Description = Resources.AppUninstallNameHelp, Required = true };

        public UninstallAppCommand() : base("uninstall-app", Resources.LegacyUninstallAppHelp)
        {
            Options.Add(nameOption);
        }

        public override string DeprecationMessage => Resources.LegacyUninstallAppDeprecated;

        protected override string GetAppName(ParseResult parseResult) => parseResult.GetValue(nameOption);
    }
}
