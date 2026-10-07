using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Deploy
{
    /// <summary>
    /// <c>deploy agent [name]</c>: creates and deploys an Infrastructure Agent.
    /// </summary>
    class DeployAgentCommand : DeployEnvironmentCommandBase
    {
        private readonly Argument<string> nameArgument = new("name")
        {
            Description = Resources.ArgAgentNameHelp,
            Arity = ArgumentArity.ZeroOrOne
        };

        public DeployAgentCommand() : base("agent", Resources.NounDeployAgentHelp, isInfrastructureAgent: true, includeNameOption: false)
        {
            Arguments.Add(nameArgument);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
