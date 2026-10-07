using Cmf.CustomerPortal.Sdk.Common;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Publish
{
    /// <summary>
    /// <c>publish installationpackage &lt;path&gt;</c>: publishes Installation Packages.
    /// </summary>
    class PublishInstallationPackageCommand : PublishInstallationPackageCommandBase
    {
        private readonly Argument<FileSystemInfo> pathArgument = new Argument<FileSystemInfo>("path") { Description = Resources.PublishPackagePathHelp }.AcceptExistingOnly();

        public PublishInstallationPackageCommand() : base("installationpackage", Resources.NounPublishInstallationPackageHelp)
        {
            Arguments.Add(pathArgument);
        }

        protected override FileSystemInfo GetPath(ParseResult parseResult) => parseResult.GetValue(pathArgument);
    }
}
