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
            Add(new Option<string>(new[] { "--id", "-n" }, Resources.INFRASTRUCTUREFROMTEMPLATE_NAME_HELP));
            Add(new Option<string>(new[] { "--agent-name", "-a" }, Resources.INFRASTRUCTUREFROMTEMPLATE_AGENTNAME_HELP));

            Add(new Option<string>(new[] { "--site", "-s", }, Resources.INFRASTRUCTURE_SITE_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--domain", "-d", }, Resources.INFRASTRUCTURE_DOMAIN_HELP)
            {
                IsRequired = true
            });

            Handler = CommandHandler.Create(typeof(CreateInfrastructureCommand).GetMethod(nameof(CreateInfrastructureCommand.CreateInfrastructureHandler)), this);
        }

        public async Task CreateInfrastructureHandler(bool verbose, string id, string agentName, string site, string domain)
        {
            // get new environment handler and run it
            CreateSession(verbose);
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(id, agentName, site, domain);
        }
    }
}
