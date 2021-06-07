using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;
using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class PublishCommand : BaseCommand
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

            var replaceTokensOption = new Option<string[]>(new[] { "--replace-tokens" }, "Replace the tokens specified in the input files using the proper syntax (e.g. #{MyToken}#) with the specified values.")
            {
                AllowMultipleArgumentsPerToken = true
            };
            replaceTokensOption.AddSuggestions(new string[] { "MyToken=value MyToken2=value2" });
            Add(replaceTokensOption);

            Handler = CommandHandler.Create(typeof(PublishCommand).GetMethod(nameof(PublishCommand.PublishHandler)), this);
        }

        public async Task PublishHandler(bool verbose, FileSystemInfo path, string[] replaceTokens)
        {
            // get new environment handler and run it
            var session = new Session(verbose);
            AddManifestsHandler newEnvironmentHandler = new AddManifestsHandler(new CustomerPortalClient(session), session);
            await newEnvironmentHandler.Run(path, replaceTokens);
        }
    }
}
