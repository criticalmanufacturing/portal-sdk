using Cmf.CustomerPortal.Sdk.Common;
using Cmf.CustomerPortal.Sdk.Common.Handlers;
using Cmf.CustomerPortal.Sdk.Powershell.Base;
using Cmf.CustomerPortal.Sdk.Powershell.Extensions;
using System.IO;
using System.Management.Automation;
using System.Threading.Tasks;

namespace Cmf.CustomerPortal.Sdk.Powershell
{
    [Cmdlet(VerbsCommon.Add, "Manifests")]
    public class AddManifests : BaseCmdlet<AddManifestsHandler>
    {
        private ReplaceTokensParameterExtension ReplaceTokensExtension;

        [Parameter(
            HelpMessage = Resources.PublishManifestsPathHelp,
            Mandatory = true
        )]
        public string Path { get; set; }

        [Parameter(
            HelpMessage = Resources.PublishManifestsDatagroupHelp,
            Mandatory = false
        )]
        public string Datagroup { get; set; }

        protected override IParameterExtension ExtendWith()
        {
            ReplaceTokensExtension = new ReplaceTokensParameterExtension();
            return ReplaceTokensExtension;
        }

        protected async override Task ProcessRecordAsync()
        {
            AddManifestsHandler addManifestsHandler = ServiceLocator.Get<AddManifestsHandler>();
            await addManifestsHandler.Run(new DirectoryInfo(Path), Datagroup, ReplaceTokensExtension.GetTokens());
        }
    }
}
