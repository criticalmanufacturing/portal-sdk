using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>undeploy</c> command. Superseded by <c>undeploy env</c>.
    /// </summary>
    class UndeployCommand : UndeployEnvironmentCommandBase
    {
        private readonly Option<string> nameOption = new("--name", "-n") { Description = Resources.CustomerEnvironmentNameHelp, Required = true };

        public UndeployCommand() : base("undeploy", Resources.LegacyUndeployHelp)
        {
            Options.Add(nameOption);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameOption);
    }
}
