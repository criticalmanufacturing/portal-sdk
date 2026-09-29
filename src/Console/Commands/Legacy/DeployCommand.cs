using Cmf.CustomerPortal.Sdk.Console.Commands.Deploy;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>deploy</c> command. Superseded by <c>deploy env</c>.
    /// </summary>
    class DeployCommand : DeployEnvironmentCommandBase
    {
        public DeployCommand() : base("deploy", "Creates and deploys a new Customer Environment", isInfrastructureAgent: false, includeNameOption: true)
        {
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(CommonParameters.Name);
    }
}
