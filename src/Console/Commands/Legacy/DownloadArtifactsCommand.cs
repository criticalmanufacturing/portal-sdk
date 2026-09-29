using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Download;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>download-artifacts</c> command. Superseded by <c>download artifacts</c>.
    /// </summary>
    class DownloadArtifactsCommand : DownloadArtifactsCommandBase
    {
        private readonly Option<string> nameOption = new("--name", "-n") { Description = Resources.DeploymentNameHelp, Required = true };

        public DownloadArtifactsCommand() : base("download-artifacts", Resources.LegacyDownloadArtifactsHelp)
        {
            Options.Add(nameOption);
        }

        public override string DeprecationMessage => Resources.LegacyDownloadArtifactsDeprecated;

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameOption);
    }
}
