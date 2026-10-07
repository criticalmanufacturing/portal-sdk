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

        public UndeployEnvironmentCommand() : base("env", Resources.NounUndeployEnvironmentHelp)
        {
            Aliases.Add("environment");
            Arguments.Add(nameArgument);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
