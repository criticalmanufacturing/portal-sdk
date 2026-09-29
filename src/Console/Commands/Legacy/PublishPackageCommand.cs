using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Console.Commands.Publish;
using System.CommandLine;
using System.IO;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Legacy
{
    /// <summary>
    /// Legacy <c>publish-package</c> command. Superseded by <c>publish installationpackage</c>.
    /// </summary>
    class PublishPackageCommand : PublishInstallationPackageCommandBase
    {
        private readonly Option<FileSystemInfo> pathOption = new Option<FileSystemInfo>("--path", "-p") { Description = Resources.PublishPackagePathHelp, Required = true }.AcceptExistingOnly();

        public PublishPackageCommand() : base("publish-package", Resources.LegacyPublishPackageHelp)
        {
            Options.Add(pathOption);
        }

        protected override FileSystemInfo GetPath(ParseResult parseResult) => parseResult.GetValue(pathOption);
    }
}
