using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DownloadArtifactsCommand : BaseCommand
    {
        public DownloadArtifactsCommand() : this("download-artifacts", "Downloads all artifacts of a specific Customer Environment")
        {
        }

        public DownloadArtifactsCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.DEPLOYMENT_NAME_HELP)
            {
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Handler = CommandHandler.Create(typeof(DownloadArtifactsCommand).GetMethod(nameof(DownloadArtifactsCommand.DownloadHandler)), this);
        }

        public async Task DownloadHandler(bool verbose, string name, DirectoryInfo output)
        {
            // get artifacts downloader handler and run it
            CreateSession(verbose);
            DownloadArtifactsHandler handler = ServiceLocator.Get<DownloadArtifactsHandler>();
            await handler.Run(name, output);
        }
    }
}
