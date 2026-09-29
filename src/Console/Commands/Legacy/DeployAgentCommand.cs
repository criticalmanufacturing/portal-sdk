using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>deployagent</c> command. Superseded by <c>deploy agent</c>.
    /// </summary>
    class DeployAgentCommand : DeployEnvironmentCommandBase
    {
        public DeployAgentCommand() : base("deployagent", Resources.LegacyDeployAgentHelp, isInfrastructureAgent: true, includeNameOption: true)
        {
        }

        public override string DeprecationMessage => Resources.LegacyDeployAgentDeprecated;

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(CommonParameters.Name);
    }
}
