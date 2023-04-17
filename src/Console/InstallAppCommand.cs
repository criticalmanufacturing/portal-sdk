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
                Argument = new Argument<FileInfo>().ExistingOnly(),
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));
            
            Handler = CommandHandler.Create(typeof(InstallAppCommand).GetMethod(nameof(InstallAppCommand.InstallHandler)), this);
        }

        protected override IEnumerable<IOptionExtension> ExtendWithRange()
        {
            return new List<IOptionExtension>
            {
                new ReplaceTokensExtension(),
                new CommonParametersExtension()
            };
        }

        public async Task InstallHandler(bool verbose, string name, string customerEnvironment, string license, FileInfo parameters, DirectoryInfo output)
        {
            // get new environment handler and run it
            CreateSession(verbose);

            InstallAppHandler installAppHandler = ServiceLocator.Get<InstallAppHandler>();
            await installAppHandler.Run(name, customerEnvironment, license, parameters, output);
        }
    }
}
