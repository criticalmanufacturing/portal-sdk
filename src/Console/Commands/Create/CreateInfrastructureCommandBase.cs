using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Create
{
    /// <summary>
    /// Options and action shared by the commands that create a Customer Infrastructure.
    /// Subclasses only decide how the Customer Infrastructure name is supplied.
    /// </summary>
    abstract class CreateInfrastructureCommandBase : BaseCommand
    {
        private readonly Option<string> siteOption = new("--site", "-s") { Description = Resources.InfrastructureSiteHelp };

        private readonly Option<string> customerOption = new("--customer", "-c") { Description = Resources.InfrastructureCustomerHelp };

        private readonly Option<bool> ignoreIfExistsOption = new("--ignore-if-exists") { Description = Resources.InfrastructureIgnoreIfExistsHelp };

        private readonly Option<FileInfo> parametersOption = new Option<FileInfo>("--parameters", "-params") { Description = Resources.InfrastructureParametersPathHelp }.AcceptExistingOnly();

        protected CreateInfrastructureCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(siteOption);
            Options.Add(customerOption);
            Options.Add(ignoreIfExistsOption);
            Options.Add(parametersOption);

            SetAction(CreateInfrastructureHandler);
        }

        /// <summary>
        /// Gets the name of the Customer Infrastructure to create.
        /// </summary>
        protected abstract string GetName(ParseResult parseResult);

        private async Task CreateInfrastructureHandler(ParseResult parseResult)
        {
            // get new infrastructure handler and run it
            CreateSession(parseResult);
            NewInfrastructureHandler handler = ServiceLocator.Get<NewInfrastructureHandler>();
            await handler.Run(
                GetName(parseResult),
                parseResult.GetValue(siteOption),
                parseResult.GetValue(customerOption),
                parseResult.GetValue(ignoreIfExistsOption),
                parseResult.GetValue(parametersOption));
        }
    }
}
