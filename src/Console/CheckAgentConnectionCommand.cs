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
        public CheckAgentConnectionCommand() : this("checkagentconnection", "Check is an Infrastructure Agent is connected")
        {
        }

        public CheckAgentConnectionCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--agent-name", "--name", "-n", }, Resources.GETAGENTCONNECTION_NAME_HELP));

            Handler = CommandHandler.Create(typeof(CheckAgentConnectionCommand).GetMethod(nameof(CheckAgentConnectionCommand.CheckAgentConnectionHandler)), this);
        }

        public async Task CheckAgentConnectionHandler(bool verbose, string agentName)
        {
            // get GetAgentConnectionHandler and run it
            var session = new Session(verbose);
            GetAgentConnectionHandler getAgentConnectionHandler = new GetAgentConnectionHandler(new CustomerPortalClient(session), session);
            bool result = await getAgentConnectionHandler.Run(agentName);

            System.Console.WriteLine(result);
        }
    }
}
