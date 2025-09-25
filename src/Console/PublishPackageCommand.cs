using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishPackageCommand : BaseCommand
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
            Add(new Option<string>(new string[] { "--datagroup", "-dg" }, Resources.PUBLISHPACKAGE_DATAGROUP_HELP)
            {
                Argument = new Argument<string>(),
                IsRequired = false
            });

            Handler = CommandHandler.Create(typeof(PublishPackageCommand).GetMethod(nameof(PublishPackageCommand.PublishPackageHandler)), this);
        }

        public async Task PublishPackageHandler(bool verbose, FileSystemInfo path, string datagroup)
        {
            // get new environment handler and run it
            CreateSession(verbose);
            PublishPackageHandler newEnvironmentHandler = ServiceLocator.Get<PublishPackageHandler>();
            await newEnvironmentHandler.Run(path.FullName, datagroup);
        }
    }
}
