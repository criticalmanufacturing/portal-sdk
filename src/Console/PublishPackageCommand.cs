using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishPackageCommand : ReplaceTokensBaseCommand
    {
        public PublishPackageCommand() : this("publish-package", "Publishes a Deployment Package into Customer Portal")
        {
        }

        public PublishPackageCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<FileSystemInfo>(new string[] { "--path", "-p" }, Resources.PUBLISHPACKAGE_PATH_HELP)
            {
                Argument = new Argument<FileSystemInfo>().ExistingOnly(),
                IsRequired = true
            });

            Handler = CommandHandler.Create(typeof(PublishPackageCommand).GetMethod(nameof(PublishPackageCommand.PublishPackageHandler)), this);
        }

        public async Task PublishPackageHandler(bool verbose, FileSystemInfo path)
        {
            // get new environment handler and run it
            var session = CreateSession(verbose);
            PublishPackageHandler newEnvironmentHandler = new PublishPackageHandler(new CustomerPortalClient(session), session);
            await newEnvironmentHandler.Run(path);
        }
    }
}
