using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class UninstallAppCommand : BaseCommand
    {
        public UninstallAppCommand() : this("uninstall-app", "Uninstalls an app from a customer environment version")
        {
        }

        public UninstallAppCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.APP_UNINSTALL_NAME_HELP)
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--customer-environment", "-ce" }, Resources.CUSTOMER_ENVIRONMENT_NAME_HELP)
            {
                IsRequired = true
            });
            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemove", "-tovr" }, Resources.APP_UNINSTALL_REMOVE_HELP));

            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemoveVolumes", "-tovrv" }, Resources.APP_UNINSTALL_REMOVE_VOLUMES_HELP));
            Add(new Option<double?>(new[] { "--timeout", "-to" }, Resources.APP_UNINSTALLATION_TIMEOUT)
            {
                IsRequired = false
            });

            Add(new Option<double?>(new[] { "--timeoutToGetSomeMBMsg", "-tombm" }, Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE)
            {
                IsRequired = false
            });

            Handler = CommandHandler.Create(typeof(UninstallAppCommand).GetMethod(nameof(UninstallAppCommand.UninstallHandler)), this);
        }

        public async Task UninstallHandler(bool verbose, string name, string customerEnvironment, bool terminateOtherVersionsRemove, bool terminateOtherVersionsRemoveVolumes, double? timeout, double? timeoutToGetSomeMBMsg = null)
        {
            CreateSession(verbose);
            UninstallAppHandler uninstallAppHandler = ServiceLocator.Get<UninstallAppHandler>();
            await uninstallAppHandler.Run(name, customerEnvironment, terminateOtherVersionsRemove, terminateOtherVersionsRemoveVolumes, timeout, timeoutToGetSomeMBMsg);
        }
    }
}