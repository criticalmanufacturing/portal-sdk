using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Publish;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>publish</c> command. Superseded by <c>publish deploymentpackage</c>.
    /// </summary>
    class PublishCommand : PublishDeploymentPackageCommandBase
    {
        private readonly Option<FileSystemInfo> pathOption = new Option<FileSystemInfo>("--path", "-p") { Description = Resources.PublishManifestsPathHelp, Required = true }.AcceptExistingOnly();

        public PublishCommand() : base("publish", Resources.LegacyPublishHelp)
        {
            Options.Add(pathOption);
        }

        public override string DeprecationMessage => Resources.LegacyPublishDeprecated;

        protected override FileSystemInfo GetPath(ParseResult parseResult) => parseResult.GetValue(pathOption);
    }
}
