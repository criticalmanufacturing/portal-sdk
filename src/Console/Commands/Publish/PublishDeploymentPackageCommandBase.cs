using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Console.Base;
using Cmf.CustomerPortal.Sdk.Console.Extensions;
using System.CommandLine;
using System.IO;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Console.Commands.Publish
{
    /// <summary>
    /// Options and action shared by the commands that publish Deployment Package manifests.
    /// Subclasses only decide how the path is supplied.
    /// </summary>
    abstract class PublishDeploymentPackageCommandBase : BaseCommand
    {
        private readonly Option<string> datagroupOption = new("--datagroup", "-dg") { Description = Resources.PublishManifestsDatagroupHelp, Required = false };

        private readonly ReplaceTokensExtension replaceTokens;

        protected PublishDeploymentPackageCommandBase(string name, string description) : base(name, description)
        {
            Options.Add(datagroupOption);
            replaceTokens = Use(new ReplaceTokensExtension());

            SetAction(PublishHandler);
        }

        /// <summary>
        /// Gets the manifest file, or the folder with the manifest files, to publish.
        /// </summary>
        protected abstract FileSystemInfo GetPath(ParseResult parseResult);

        private async Task PublishHandler(ParseResult parseResult)
        {
            // get add manifests handler and run it
            CreateSession(parseResult);
            AddManifestsHandler addManifestsHandler = ServiceLocator.Get<AddManifestsHandler>();
            await addManifestsHandler.Run(GetPath(parseResult), parseResult.GetValue(datagroupOption), parseResult.GetValue(replaceTokens.ReplaceTokens));
        }
    }
}
