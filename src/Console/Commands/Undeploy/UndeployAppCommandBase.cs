using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Undeploy
{
    /// <summary>
    /// Options and action shared by the commands that uninstall an App from a Customer Environment.
    /// Subclasses only decide how the App name is supplied.
    /// </summary>
    abstract class UndeployAppCommandBase : BaseCommand
    {
        private readonly Option<string> customerEnvironmentOption = new("--customer-environment", "-ce") { Description = Resources.CustomerEnvironmentNameHelp, Required = true };

        // kept for backwards compatibility, removal is always enabled
        private readonly Option<bool> terminateOtherVersionsRemoveOption = new("--terminateOtherVersionsRemove", "-tovr") { Description = Resources.AppUninstallRemoveHelp };

        private readonly Option<bool> removeVolumesOption = new("--terminateOtherVersionsRemoveVolumes", "-tovrv", "--removeVolumes") { Description = Resources.AppUninstallRemoveVolumesHelp };

        private readonly Option<bool> undeployOption = new("--undeploy") { Description = Resources.AppUndeployHelp };

        private readonly Option<double?> timeoutOption = new("--timeout", "-to") { Description = Resources.AppUninstallationTimeoutHelp, Required = false };

        private readonly Option<double?> timeoutToGetSomeMBMsgOption = new("--timeoutToGetSomeMBMsg", "-tombm") { Description = Resources.DeploymentTimeoutMinutesToGetSomeMBMessageHelp, Required = false };

        protected UndeployAppCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(customerEnvironmentOption);
            Options.Add(terminateOtherVersionsRemoveOption);
            Options.Add(removeVolumesOption);
            Options.Add(undeployOption);
            Options.Add(timeoutOption);
            Options.Add(timeoutToGetSomeMBMsgOption);

            SetAction(UninstallHandler);
        }

        /// <summary>
        /// Gets the name of the App to uninstall.
        /// </summary>
        protected abstract string GetAppName(ParseResult parseResult);

        private async Task UninstallHandler(ParseResult parseResult)
        {
            CreateSession(parseResult);
            UninstallAppHandler uninstallAppHandler = ServiceLocator.Get<UninstallAppHandler>();
            await uninstallAppHandler.Run(
                GetAppName(parseResult),
                parseResult.GetValue(customerEnvironmentOption),
                parseResult.GetValue(removeVolumesOption),
                parseResult.GetValue(undeployOption),
                parseResult.GetValue(timeoutOption),
                parseResult.GetValue(timeoutToGetSomeMBMsgOption));
        }
    }
}
