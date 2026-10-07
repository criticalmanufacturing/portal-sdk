using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Publish
{
    /// <summary>
    /// <c>publish deploymentpackage &lt;path&gt;</c>: publishes Deployment Package manifests.
    /// </summary>
    class PublishDeploymentPackageCommand : PublishDeploymentPackageCommandBase
    {
        private readonly Argument<FileSystemInfo> pathArgument = new Argument<FileSystemInfo>("path") { Description = Resources.PublishManifestsPathHelp }.AcceptExistingOnly();

        public PublishDeploymentPackageCommand() : base("deploymentpackage", Resources.NounPublishDeploymentPackageHelp)
        {
            Arguments.Add(pathArgument);
        }

        protected override FileSystemInfo GetPath(ParseResult parseResult) => parseResult.GetValue(pathArgument);
    }
}
