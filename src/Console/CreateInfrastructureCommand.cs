using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class CreateInfrastructureCommand : BaseCommand
    {
        public CreateInfrastructureCommand() : this("createinfrastructure", "Creates a customer Infrastructure")
        {
        }

        public CreateInfrastructureCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.INFRASTRUCTUREFROMTEMPLATE_NAME_HELP));
            Add(new Option<string>(new[] { "--agent-name", "-a" }, Resources.INFRASTRUCTUREFROMTEMPLATE_AGENTNAME_HELP));

            Add(new Option<string>(new[] { "--site", "-s", }, Resources.INFRASTRUCTURE_SITE_HELP));
            Add(new Option<string>(new[] { "--customer", "-c", }, Resources.INFRASTRUCTURE_CUSTOMER_HELP));

            Add(new Option<string>(new[] { "--domain", "-d", }, Resources.INFRASTRUCTURE_DOMAIN_HELP)
            {
                IsRequired = true
            });

            Handler = CommandHandler.Create((DeployParameters x) => CreateInfrastructureHandler(x));
        }

        public async Task CreateInfrastructureHandler(DeployParameters parameters)
        {
            // get new environment handler and run it
            CreateSession(parameters.Verbose);
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(parameters.Name, parameters.AgentName, parameters.Site, parameters.Customer, parameters.Domain);
        }
    }
}
