using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Cancel
{
    /// <summary>
    /// <c>cancel env &lt;name&gt;</c>: cancels the in-progress deployment of a Customer Environment.
    /// </summary>
    class CancelEnvironmentCommand : BaseCommand
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

        public CancelEnvironmentCommand() : base("env", Resources.NounCancelEnvironmentHelp)
        {
            Aliases.Add("environment");
            Arguments.Add(nameArgument);

            SetAction(CancelHandler);
        }

        private async Task CancelHandler(ParseResult parseResult)
        {
            CreateSession(parseResult);
            CancelEnvironmentDeploymentHandler cancelEnvironmentDeploymentHandler = ServiceLocator.Get<CancelEnvironmentDeploymentHandler>();
            await cancelEnvironmentDeploymentHandler.Run(parseResult.GetValue(nameArgument));
        }
    }
}
