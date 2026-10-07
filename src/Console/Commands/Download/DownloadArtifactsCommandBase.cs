using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Download
{
    /// <summary>
    /// Options and action shared by the commands that download the artifacts of a Customer Environment.
    /// Subclasses only decide how the Customer Environment name is supplied.
    /// </summary>
    abstract class DownloadArtifactsCommandBase : BaseCommand
    {
        private readonly Option<DirectoryInfo> outputOption = new("--output", "-o") { Description = Resources.DeploymentOutputDirHelp };

        protected DownloadArtifactsCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(outputOption);

            SetAction(DownloadHandler);
        }

        /// <summary>
        /// Gets the name of the Customer Environment whose artifacts are downloaded.
        /// </summary>
        protected abstract string GetName(ParseResult parseResult);

        private async Task DownloadHandler(ParseResult parseResult)
        {
            // get artifacts downloader handler and run it
            CreateSession(parseResult);
            DownloadArtifactsHandler handler = ServiceLocator.Get<DownloadArtifactsHandler>();
            await handler.Run(GetName(parseResult), parseResult.GetValue(outputOption));
        }
    }
}
