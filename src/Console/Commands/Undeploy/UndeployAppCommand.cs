using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy
{
    /// <summary>
    /// <c>undeploy app &lt;name&gt;</c>: uninstalls an App from a Customer Environment.
    /// </summary>
    class UndeployAppCommand : UndeployAppCommandBase
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.AppUninstallNameHelp };

        public UndeployAppCommand() : base("app", Resources.NounUndeployAppHelp)
        {
            Aliases.Add("application");
            Arguments.Add(nameArgument);
        }

        protected override string GetAppName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
