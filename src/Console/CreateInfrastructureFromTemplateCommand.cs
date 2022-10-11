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
            Add(new Option<bool>(new[] { "--force", "-f", }, Resources.INFRASTRUCTURE_FORCE_HELP));
            Add(new Option<int>(new[] { "--timeout", "-to", }, Resources.INFRASTRUCTURE_CREATION_SECONDS_TIMEOUT_HELP));

            Handler = CommandHandler.Create((DeployParameters x) => CreateInfrastructureFromTemplateHandler(x));
        }

        public async Task CreateInfrastructureFromTemplateHandler(DeployParameters parameters)
        {
            // get new environment handler and run it
            CreateSession(parameters.Verbose);
            NewInfrastructureFromTemplateHandler newInfrastructureFromTemplateHandler = ServiceLocator.Get<NewInfrastructureFromTemplateHandler>();
            await newInfrastructureFromTemplateHandler.Run(parameters.Name, parameters.TemplateName, parameters.Force, parameters.Timeout);
        }
    }
}
