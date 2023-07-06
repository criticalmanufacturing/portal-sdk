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
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.INFRASTRUCTURE_NAME_HELP));

            Add(new Option<string>(new[] { "--site", "-s", }, Resources.INFRASTRUCTURE_SITE_HELP));
            Add(new Option<string>(new[] { "--customer", "-c", }, Resources.INFRASTRUCTURE_CUSTOMER_HELP));
            Add(new Option<bool>(new[] { "--ignore-if-exists", }, Resources.INFRASTRUCTURE_IGNORE_IF_EXISTS_HELP));
            Handler = CommandHandler.Create((CreateInfrastructureParameters x) => CreateInfrastructureHandler(x));
        }

        public async Task CreateInfrastructureHandler(CreateInfrastructureParameters parameters)
        {
            // get new environment handler and run it
            CreateSession(parameters.Verbose);
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(parameters.Name, parameters.Site, parameters.Customer, parameters.IgnoreIfExists);
        }
    }
}
