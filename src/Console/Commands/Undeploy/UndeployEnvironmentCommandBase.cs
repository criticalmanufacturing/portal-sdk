using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy
{
    /// <summary>
    /// Options and action shared by the commands that undeploy a Customer Environment.
    /// Subclasses only decide how the Customer Environment name is supplied.
    /// </summary>
    abstract class UndeployEnvironmentCommandBase : BaseCommand
    {
        private readonly Option<bool> forceOption = new("--force", "-f") { Description = Resources.UndeploymentForceHelp };

        protected UndeployEnvironmentCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(forceOption);

            SetAction(UndeployHandler);
        }

        /// <summary>
        /// Gets the name of the Customer Environment to undeploy.
        /// </summary>
        protected abstract string GetName(ParseResult parseResult);

        private async Task UndeployHandler(ParseResult parseResult)
        {
            // get undeploy environment handler and run it
            CreateSession(parseResult);
            UndeployEnvironmentHandler undeployEnvironmentHandler = ServiceLocator.Get<UndeployEnvironmentHandler>();
            await undeployEnvironmentHandler.Run(GetName(parseResult), parseResult.GetValue(forceOption));
        }
    }
}
