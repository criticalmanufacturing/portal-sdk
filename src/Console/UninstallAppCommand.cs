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
        public UninstallAppCommand() : this("uninstallApp", "[Preview] Uninstalls an app from a customer environment version")
        {
        }

        public UninstallAppCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--appName", }, Resources.CUSTOMER_ENVIRONMENT_APPLICATION_PACKAGE_NAME_HELP)
            {
                IsRequired = true
            });
            Add(new Option<string>(new[] { "--customerEnvironmentName", }, Resources.CUSTOMER_ENVIRONMENT_NAME_HELP)
            {
                IsRequired = true
            });
            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemove", "-tovr" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_HELP));

            Add(new Option<bool>(new[] { "--terminateOtherVersionsRemoveVolumes", "-tovrv" }, Resources.DEPLOYMENT_TERMINATE_OTHER_VERSIONS_REMOVE_VOLUMES_HELP));


            Handler = CommandHandler.Create((DeployParameters x) => UninstallAppHandler(x));
        }

        public async Task UninstallAppHandler(DeployParameters parameters)
        {
            CreateSession(parameters.Verbose);
            UninstallAppHandler undeployEnvironmentHandler = ServiceLocator.Get<UninstallAppHandler>();
            await undeployEnvironmentHandler.Run(parameters.AppName, parameters.CustomerEnvironmentName, parameters.TerminateOtherVersionsRemove, parameters.TerminateOtherVersionsRemoveVolumes);
        }
    }
}