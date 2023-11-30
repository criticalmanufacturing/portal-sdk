using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class InstallAppCommand : BaseCommand
    {
        public InstallAppCommand() : this("install-app", "Install an App in a previously deployed Convergence environment.")
        {
        }

        public InstallAppCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n", }, Resources.APP_NAME_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--app-version", "-av" }, Resources.APP_VERSION_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--customer-environment", "-ce", }, Resources.APP_CUSTOMER_ENVIRONMENT_HELP)
            {
                IsRequired = true
            });

            Add(new Option<string>(new[] { "--license", "-lic", }, Resources.APP_LICENSE_HELP)
            {
                IsRequired = true
            });

            Add(new Option<FileInfo>(new string[] { "--parameters", "-params" }, Resources.APP_PARAMETERS_PATH_HELP)
            {
                Argument = new Argument<FileInfo>().ExistingOnly()
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Add(new Option<double?>(new[] { "--timeout" , "-to" }, Resources.APP_INSTALLATION_TIMEOUT));

            Add(new Option<double?>(new[] { "--timeoutToGetSomeMBMsg", "-tombm" }, Resources.DEPLOYMENT_TIMEOUT_MINUTES_TO_GET_SOME_MB_MESSAGE)
            {
                IsRequired = false
            });

            Handler = CommandHandler.Create(typeof(InstallAppCommand).GetMethod(nameof(InstallAppCommand.InstallHandler)), this);
        }

        protected override IEnumerable<IOptionExtension> ExtendWithRange()
        {
            return new List<IOptionExtension>
            {
                new ReplaceTokensExtension()
            };
        }

        public async Task InstallHandler(bool verbose, string name, string appVersion, string customerEnvironment, string license, FileInfo parameters, string[] replaceTokens, DirectoryInfo output, double? timeout, double? timeoutToGetSomeMBMessage = null)
        {
            // get new environment handler and run it
            CreateSession(verbose);

            InstallAppHandler installAppHandler = ServiceLocator.Get<InstallAppHandler>();
            await installAppHandler.Run(name, appVersion, customerEnvironment, license, parameters, replaceTokens, output, timeout, timeoutToGetSomeMBMessage);
        }
    }
}
