using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class UndeployCommand : BaseCommand
    {
        public UndeployCommand() : this("undeploy", "[Preview] Creates a new CustomerEnvironment's version and terminates the other versions, removing deployments")
        {
        }

        public UndeployCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n", }, Resources.CUSTOMER_ENVIRONMENT_NAME_HELP)
            {
                IsRequired = true
            });

            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemoveVolumes", "-tovrv" }, Resources.UNDEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP));

            Handler = CommandHandler.Create((DeployParameters x) => DeployHandler(x));
        }

        public async Task DeployHandler(DeployParameters parameters)
        {
            // get undeploy environment handler and run it
            CreateSession(parameters.Verbose);
            UndeployEnvironmentHandler undeployEnvironmentHandler = ServiceLocator.Get<UndeployEnvironmentHandler>();
            await undeployEnvironmentHandler.Run(parameters.Name, parameters.TerminateOtherVersionsRemoveVolumes);
        }
    }
}