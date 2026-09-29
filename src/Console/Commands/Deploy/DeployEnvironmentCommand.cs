using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Deploy
{
    /// <summary>
    /// <c>deploy env [name]</c>: creates and deploys a Customer Environment.
    /// </summary>
    class DeployEnvironmentCommand : DeployEnvironmentCommandBase
    {
        private readonly Argument<string> nameArgument = new("name")
        {
            Description = Resources.ArgEnvironmentNameHelp,
            Arity = ArgumentArity.ZeroOrOne
        };

        public DeployEnvironmentCommand() : base("env", Resources.NounDeployEnvironmentHelp, isInfrastructureAgent: false, includeNameOption: false)
        {
            Aliases.Add("environment");
            Arguments.Add(nameArgument);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
