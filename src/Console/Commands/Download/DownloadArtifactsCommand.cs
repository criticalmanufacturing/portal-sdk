using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Download
{
    /// <summary>
    /// <c>download artifacts &lt;name&gt;</c>: downloads the artifacts of a Customer Environment.
    /// </summary>
    class DownloadArtifactsCommand : DownloadArtifactsCommandBase
    {
        private readonly Argument<string> nameArgument = new("name") { Description = Resources.ArgExistingEnvironmentNameHelp };

        public DownloadArtifactsCommand() : base("artifacts", Resources.NounDownloadArtifactsHelp)
        {
            Arguments.Add(nameArgument);
        }

        protected override string GetName(ParseResult parseResult) => parseResult.GetValue(nameArgument);
    }
}
