using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DownloadDeployArtifactCommand : BaseCommand
    {
        public DownloadDeployArtifactCommand() : this("downloaddeployartifact", "Downloads a Customer Environment artifact")
        {
        }

        public DownloadDeployArtifactCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.DEPLOYMENT_NAME_HELP)
            {
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Handler = CommandHandler.Create(typeof(DownloadDeployArtifactCommand).GetMethod(nameof(DownloadDeployArtifactCommand.DownloadDeployArtifactHandler)), this);
        }

        public async Task DownloadDeployArtifactHandler(bool verbose, string name, DirectoryInfo output)
        {
            // get download artifact handler and run it
            CreateSession(verbose);
            DownloaderDeployArtifactHandler handler = ServiceLocator.Get<DownloaderDeployArtifactHandler>();
            await handler.Run(name, output);
        }
    }
}
