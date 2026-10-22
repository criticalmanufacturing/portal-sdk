using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy
{
    /// <summary>
    /// <c>undeploy env &lt;name&gt;</c>: undeploys a Customer Environment.
    /// </summary>
    class UndeployEnvironmentCommand : UndeployEnvironmentCommandBase
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

        private readonly Option<bool> purgeOption = new("--purge") { Description = Resources.UndeploymentPurgeHelp };

        public UndeployEnvironmentCommand() : base("env", Resources.NounUndeployEnvironmentHelp)
        {
            Aliases.Add("environment");
            Arguments.Add(nameArgument);
            Options.Add(purgeOption);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);

        protected override bool GetPurge(ParseResult parseResult) => parseResult.GetValue(purgeOption);
    }
}
