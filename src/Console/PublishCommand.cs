using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishCommand : ReplaceTokensCommand
    {

        public PublishCommand() : this("publish", "Publishes one or more Deployment Package(s) into Customer Portal")
        {
        }

        public PublishCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option(new string[] { "--path", "-p" }, Resources.PUBLISHMANIFESTS_PATH_HELP)
            {
                Argument = new Argument<FileSystemInfo>().ExistingOnly(),
                IsRequired = true
            });

            Handler = CommandHandler.Create(typeof(PublishCommand).GetMethod(nameof(PublishCommand.PublishHandler)), this);
        }

        public async Task PublishHandler(bool verbose, FileSystemInfo path, string[] replaceTokens)
        {
            // get new environment handler and run it
            var session = CreateSession(verbose);
            AddManifestsHandler newEnvironmentHandler = new AddManifestsHandler(new CustomerPortalClient(session), session);
            await newEnvironmentHandler.Run(path, replaceTokens);
        }
    }
}
