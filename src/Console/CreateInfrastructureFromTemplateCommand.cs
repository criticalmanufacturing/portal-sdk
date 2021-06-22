using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class CreateInfrastructureFromTemplateCommand : BaseCommand
    {
        public CreateInfrastructureFromTemplateCommand() : this("createinfrastructurefromtemplate", "Creates a customer Infrastructure from a Template")
        {
        }

        public CreateInfrastructureFromTemplateCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.INFRASTRUCTUREFROMTEMPLATE_NAME_HELP));
            Add(new Option<string>(new[] { "--template-name", "-t" }, Resources.INFRASTRUCTUREFROMTEMPLATE_TEMPLATENAME_HELP)
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--agent-name", "-a" }, Resources.INFRASTRUCTUREFROMTEMPLATE_AGENTNAME_HELP));

            Handler = CommandHandler.Create(typeof(CreateInfrastructureFromTemplateCommand).GetMethod(nameof(CreateInfrastructureFromTemplateCommand.CreateInfrastructureFromTemplateHandler)), this);
        }

        public async Task CreateInfrastructureFromTemplateHandler(bool verbose, string name, string agentName, string templateName)
        {
            // get new environment handler and run it
            var session = CreateSession(verbose);
            NewInfrastructureFromTemplateHandler newInfrastructureFromTemplateHandler = new NewInfrastructureFromTemplateHandler(new CustomerPortalClient(session), session);
            await newInfrastructureFromTemplateHandler.Run(name, templateName, agentName);
        }
    }
}
