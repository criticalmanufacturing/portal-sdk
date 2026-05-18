using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.Linq;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class CheckAgentConnectionCommand : BaseCommand
    {
        public CheckAgentConnectionCommand() : this("checkagentconnection", "Check if an Infrastructure Agent is connected")
        {
        }

        public CheckAgentConnectionCommand(string name, string description = null) : base(name, description)
        {
            var agentOpt = new Option<string>(new[] { "--agent-name", "--name", "-n", }, Resources.GETAGENTCONNECTION_NAME_HELP);
            Add(agentOpt);

            var customerEnvironmentOpt = new Option<string>(new[] { "--customer-environment", "-ce", }, Resources.GETAGENTCONNECTION_CUSTOMER_ENVIRONMENT_HELP);
            Add(customerEnvironmentOpt);

            AddValidator(commandResult => ValidateRequiredOptions(commandResult, agentOpt, customerEnvironmentOpt));

            Handler = CommandHandler.Create((DeployParameters x) => CheckAgentConnectionHandler(x));
        }

        public async Task CheckAgentConnectionHandler(DeployParameters parameters)
        {
            // get GetAgentConnectionHandler and run it
            CreateSession(parameters.Verbose);
            GetAgentConnectionHandler getAgentConnectionHandler = ServiceLocator.Get<GetAgentConnectionHandler>();
            bool result = await getAgentConnectionHandler.Run(parameters.AgentName, parameters.CustomerEnvironment);
            System.Console.WriteLine(result);
        }

        private static string ValidateRequiredOptions(
            CommandResult commandResult,
            Option<string> agentOpt,
            Option<string> customerEnvironmentOpt)
        {
            var agentResult = commandResult.FindResultFor(agentOpt);
            bool hasAgent = agentResult != null && agentResult.Tokens.Count > 0;

            var customerEnvironmentResult = commandResult.FindResultFor(customerEnvironmentOpt);
            bool hasCustomerEnvironment = customerEnvironmentResult != null && customerEnvironmentResult.Tokens.Count > 0;

            if (!hasAgent && !hasCustomerEnvironment)
            {
                commandResult.ErrorMessage = $"At least one of the following options must be provided: {agentOpt.Aliases.FirstOrDefault()}, {customerEnvironmentOpt.Aliases.FirstOrDefault()}";
            }
            else if (hasAgent && hasCustomerEnvironment)
            {
                commandResult.ErrorMessage = $"Only one of the following options can be provided: {agentOpt.Aliases.FirstOrDefault()}, {customerEnvironmentOpt.Aliases.FirstOrDefault()}";
            }

            return commandResult.ErrorMessage;
        }
    }
}
