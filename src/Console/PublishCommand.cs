using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishCommand : BaseCommand
    {
        public PublishCommand() : this("publish", "Publishes one or more Deployment Package(s) into Customer Portal")
        {
        }

        public PublishCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<FileSystemInfo>(new string[] { "--path", "-p" }, Resources.PUBLISHMANIFESTS_PATH_HELP)
            {
                Argument = new Argument<FileSystemInfo>().ExistingOnly(),
                IsRequired = true
            });

            Add(new Option<string>(new string[] { "--datagroup", "-dg" }, Resources.PUBLISHMANIFESTS_DATAGROUP_HELP)
            {
                Argument = new Argument<string>(),
                IsRequired = false
            });

            Handler = CommandHandler.Create(typeof(PublishCommand).GetMethod(nameof(PublishCommand.PublishHandler)), this);
        }

        protected override IOptionExtension ExtendWith()
        {
            return new ReplaceTokensExtension();
        }

        public async Task PublishHandler(bool verbose, FileSystemInfo path, string datagroup, string[] replaceTokens)
        {
            // get new environment handler and run it
            CreateSession(verbose);
            AddManifestsHandler newEnvironmentHandler = ServiceLocator.Get<AddManifestsHandler>();
            await newEnvironmentHandler.Run(path, datagroup, replaceTokens);
        }
    }
}
