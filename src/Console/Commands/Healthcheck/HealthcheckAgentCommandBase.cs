using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Healthcheck
{
    /// <summary>
    /// Options, validation and action shared by the commands that check if an Infrastructure Agent is connected.
    /// The agent is identified either by its name or by <c>--customer-environment</c>, never both.
    /// </summary>
    abstract class HealthcheckAgentCommandBase : BaseCommand
    {
        private readonly Option<string> customerEnvironmentOption = new("--customer-environment", "-ce") { Description = Resources.GetAgentConnectionCustomerEnvironmentHelp };

        protected HealthcheckAgentCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(customerEnvironmentOption);

            Validators.Add(ValidateRequiredOptions);

            SetAction(CheckAgentConnectionHandler);
        }

        /// <summary>
        /// Label of the agent name symbol, used in validation messages.
        /// </summary>
        protected abstract string AgentNameLabel { get; }

        /// <summary>
        /// Gets the parse result of the agent name symbol, or <c>null</c> when it was not provided.
        /// </summary>
        protected abstract SymbolResult GetAgentNameResult(CommandResult commandResult);

        /// <summary>
        /// Gets the name of the Infrastructure Agent.
        /// </summary>
        protected abstract string GetAgentName(ParseResult parseResult);

        private async Task CheckAgentConnectionHandler(ParseResult parseResult)
        {
            // get GetAgentConnectionHandler and run it
            CreateSession(parseResult);
            GetAgentConnectionHandler getAgentConnectionHandler = ServiceLocator.Get<GetAgentConnectionHandler>();
            bool result = await getAgentConnectionHandler.Run(GetAgentName(parseResult), parseResult.GetValue(customerEnvironmentOption));
            System.Console.WriteLine(result);
        }

        private void ValidateRequiredOptions(CommandResult commandResult)
        {
            var agentResult = GetAgentNameResult(commandResult);
            bool hasAgent = agentResult != null && agentResult.Tokens.Count > 0;

            var customerEnvironmentResult = commandResult.GetResult(customerEnvironmentOption);
            bool hasCustomerEnvironment = customerEnvironmentResult != null && customerEnvironmentResult.Tokens.Count > 0;

            if (!hasAgent && !hasCustomerEnvironment)
            {
                commandResult.AddError($"At least one of the following options must be provided: {AgentNameLabel}, {customerEnvironmentOption.Name}");
            }
            else if (hasAgent && hasCustomerEnvironment)
            {
                commandResult.AddError($"Only one of the following options can be provided: {AgentNameLabel}, {customerEnvironmentOption.Name}");
            }
        }
    }
}
