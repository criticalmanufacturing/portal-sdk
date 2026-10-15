using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Cancel
{
    /// <summary>
    /// <c>cancel app &lt;name&gt; -ce &lt;customer-environment&gt;</c>: cancels the in-progress installation of an App in a Customer Environment.
    /// </summary>
    class CancelAppCommand : BaseCommand
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgAppNameHelp };

        private readonly Option<string> customerEnvironmentOption = new("--customer-environment", "-ce") { Description = Resources.CustomerEnvironmentNameHelp, Required = true };

        public CancelAppCommand() : base("app", Resources.NounCancelAppHelp)
        {
            Aliases.Add("application");
            Arguments.Add(nameArgument);
            Options.Add(customerEnvironmentOption);

            SetAction(CancelHandler);
        }

        private async Task CancelHandler(ParseResult parseResult)
        {
            CreateSession(parseResult);
            CancelAppDeploymentHandler cancelAppDeploymentHandler = ServiceLocator.Get<CancelAppDeploymentHandler>();
            await cancelAppDeploymentHandler.Run(
                parseResult.GetValue(nameArgument),
                parseResult.GetValue(customerEnvironmentOption));
        }
    }
}
