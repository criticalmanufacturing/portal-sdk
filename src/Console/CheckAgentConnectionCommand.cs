using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
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
            Add(new Option<string>(new[] { "--name","--agent-name", "-n", }, Resources.GETAGENTCONNECTION_NAME_HELP)
            {
                IsRequired = true
            });

            Handler = CommandHandler.Create((DeployParameters x) => CheckAgentConnectionHandler(x));
        }

        public async Task CheckAgentConnectionHandler(DeployParameters parameters)
        {
            // get GetAgentConnectionHandler and run it
            CreateSession(parameters.Verbose);
            GetAgentConnectionHandler getAgentConnectionHandler = ServiceLocator.Get<GetAgentConnectionHandler>();
            bool result = await getAgentConnectionHandler.Run(parameters.AgentName);
            System.Console.WriteLine(result);
        }
    }
}
