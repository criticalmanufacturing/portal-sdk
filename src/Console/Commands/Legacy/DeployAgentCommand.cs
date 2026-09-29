using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>deployagent</c> command. Superseded by <c>deploy agent</c>.
    /// </summary>
    class DeployAgentCommand : DeployEnvironmentCommandBase
    {
        public DeployAgentCommand() : base("deployagent", "Creates and deploys a new Infrastructure Agent", isInfrastructureAgent: true, includeNameOption: true)
        {
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(CommonParameters.Name);
    }
}
