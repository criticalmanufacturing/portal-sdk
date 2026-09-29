using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;
using System.CommandLine.Parsing;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Healthcheck
{
    /// <summary>
    /// <c>healthcheck agent [agent-name] [-ce &lt;customer-environment&gt;]</c>: checks if an Infrastructure Agent is connected.
    /// </summary>
    class HealthcheckAgentCommand : HealthcheckAgentCommandBase
    {
        private readonly Argument<string> agentNameArgument = new("agent-name")
        {
            Description = Resources.GetAgentConnectionNameHelp,
            Arity = ArgumentArity.ZeroOrOne
        };

        public HealthcheckAgentCommand() : base("agent", Resources.NounHealthcheckAgentHelp)
        {
            Arguments.Add(agentNameArgument);
        }

        protected override string AgentNameLabel => $"<{agentNameArgument.Name}>";

        protected override SymbolResult GetAgentNameResult(CommandResult commandResult) => commandResult.GetResult(agentNameArgument);

        protected override string GetAgentName(ParseResult parseResult) => parseResult.GetValue(agentNameArgument);
    }
}
