using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Publish
{
    /// <summary>
    /// Options and action shared by the commands that publish Installation Packages.
    /// Subclasses only decide how the path is supplied.
    /// </summary>
    abstract class PublishInstallationPackageCommandBase : BaseCommand
    {
        private readonly Option<string> datagroupOption = new("--datagroup", "-dg") { Description = Resources.PublishPackageDatagroupHelp, Required = false };

        protected PublishInstallationPackageCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(datagroupOption);

            SetAction(PublishPackageHandler);
        }

        /// <summary>
        /// Gets the package zip file, or the folder with the package zip files, to publish.
        /// </summary>
        protected abstract FileSystemInfo GetPath(ParseResult parseResult);

        private async Task PublishPackageHandler(ParseResult parseResult)
        {
            // get publish package handler and run it
            CreateSession(parseResult);
            PublishPackageHandler publishPackageHandler = ServiceLocator.Get<PublishPackageHandler>();
            await publishPackageHandler.Run(GetPath(parseResult).FullName, parseResult.GetValue(datagroupOption));
        }
    }
}
