using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console
{
    class DownloadManifestsCommand : BaseCommand
    {
        public DownloadManifestsCommand() : this("download-manifests", "Downloads all manifests of a specific Customer Environment")
        {
        }

        public DownloadManifestsCommand(string name, string description = null) : base(name, description)
        {
            Add(new Option<string>(new[] { "--name", "-n" }, Resources.DEPLOYMENT_NAME_HELP)
            {
                IsRequired = true
            });

            Add(new Option<DirectoryInfo>(new string[] { "--output", "-o" }, Resources.DEPLOYMENT_OUTPUTDIR_HELP));

            Handler = CommandHandler.Create(typeof(DownloadManifestsCommand).GetMethod(nameof(DownloadManifestsCommand.ManifestsDownloaderHandler)), this);
        }

        public async Task ManifestsDownloaderHandler(bool verbose, string name, DirectoryInfo output)
        {
            // get manifests downloader handler and run it
            CreateSession(verbose);
            ManifestsDownloaderHandler handler = ServiceLocator.Get<ManifestsDownloaderHandler>();
            await handler.Run(name, output);
        }
    }
}
