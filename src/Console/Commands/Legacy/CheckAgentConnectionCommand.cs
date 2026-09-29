using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Healthcheck;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>checkagentconnection</c> command. Superseded by <c>healthcheck agent</c>.
    /// </summary>
    class CheckAgentConnectionCommand : HealthcheckAgentCommandBase
    {
        private readonly Option<string> agentNameOption = new("--agent-name", "--name", "-n") { Description = Resources.GetAgentConnectionNameHelp };

        public CheckAgentConnectionCommand() : base("checkagentconnection", "Check if an Infrastructure Agent is connected")
        {
            Options.Add(agentNameOption);
        }

        protected override string AgentNameLabel => agentNameOption.Name;

        protected override SymbolResult GetAgentNameResult(CommandResult commandResult) => commandResult.GetResult(agentNameOption);

        protected override string GetAgentName(ParseResult parseResult) => parseResult.GetValue(agentNameOption);
    }
}
